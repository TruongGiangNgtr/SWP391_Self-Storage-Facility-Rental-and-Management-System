# Real-SQL verification. Run explicitly; only uniquely-owned fixtures in Frms_Test_CWP04.
# Uses local backend .env only in memory. Never prints connection/password/hash.
# Does not run on FRMS_EPS01_Dev or modify pre-existing business records.
$ErrorActionPreference='Stop'
Add-Type -Path 'C:\SWP391\backend\Frms.Api\bin\Release\net10.0\Microsoft.SqlServer.Server.dll'
Add-Type -Path 'C:\SWP391\backend\Frms.Api\bin\Release\net10.0\System.Memory.Data.dll'
Add-Type -Path 'C:\SWP391\backend\Frms.Api\bin\Release\net10.0\runtimes\win\lib\net9.0\Microsoft.Data.SqlClient.dll'
[void][System.Runtime.InteropServices.NativeLibrary]::Load('C:\SWP391\backend\Frms.Api\bin\Release\net10.0\runtimes\win-x64\native\Microsoft.Data.SqlClient.SNI.dll')
$taskLine=Get-Content -LiteralPath 'C:\SWP391\backend\Frms.Api\.env'|Where-Object{$_ -match '^\s*ConnectionStrings__FrmsDb\s*='}|Select-Object -First 1
$taskBuilder=[Microsoft.Data.SqlClient.SqlConnectionStringBuilder]::new($taskLine.Split('=',2)[1].Trim().Trim('"').Trim("'"))
$taskBuilder['Initial Catalog']='Frms_Test_CWP04'
$taskConn=[Microsoft.Data.SqlClient.SqlConnection]::new($taskBuilder.ConnectionString)
$taskConn.Open()
$taskPassed=0
$taskAccount=[Guid]::NewGuid()
$taskCustomer=[Guid]::NewGuid()
$taskFacility=[Guid]::NewGuid()
$taskUnitType=[Guid]::NewGuid()
$taskUnit=[Guid]::NewGuid()
$taskTrigger='trg_Test_RES001_'+[Guid]::NewGuid().ToString('N')
function Scalar([string]$Sql) {$c=$taskConn.CreateCommand();$c.CommandText=$Sql;try{return $c.ExecuteScalar()}finally{$c.Dispose()}}
function ExecSql([string]$Sql) {$c=$taskConn.CreateCommand();$c.CommandText=$Sql;try{[void]$c.ExecuteNonQuery()}finally{$c.Dispose()}}
function Check([bool]$Condition,[string]$Name) {if(-not $Condition){throw "FAIL: $Name"};$script:taskPassed++;Write-Output "PASS: $Name"}
function ClearCreated {
 ExecSql @"
DELETE dbo.AuditLog WHERE UserAccountId='$taskAccount' AND EntityType='RESERVATION';
DELETE dbo.Contract WHERE ReservationId IN(SELECT ReservationId FROM dbo.Reservation WHERE CustomerId='$taskCustomer' AND FacilityId='$taskFacility');
DELETE dbo.Invoice WHERE EntityId IN(SELECT ReservationId FROM dbo.Reservation WHERE CustomerId='$taskCustomer' AND FacilityId='$taskFacility');
DELETE dbo.Reservation WHERE CustomerId='$taskCustomer' AND FacilityId='$taskFacility';
"@
}
function Snapshot {return Scalar "SELECT CONCAT((SELECT COUNT(*) FROM dbo.Reservation WHERE CustomerId='$taskCustomer'),':',(SELECT COUNT(*) FROM dbo.Invoice i JOIN dbo.Reservation r ON r.ReservationId=i.EntityId WHERE r.CustomerId='$taskCustomer'),':',(SELECT COUNT(*) FROM dbo.AuditLog WHERE UserAccountId='$taskAccount'))"}
function MakeCommand($Connection,$Start='2026-11-01',$End='2026-11-01',$Now='2026-10-11T00:00:00',$Customer=$taskCustomer,$Facility=$taskFacility,$UnitType=$taskUnitType) {
 $c=$Connection.CreateCommand();$c.CommandType=[System.Data.CommandType]::StoredProcedure;$c.CommandText='dbo.usp_CreateReservation';$c.CommandTimeout=20
 foreach($v in @(@('@CustomerId',$Customer),@('@FacilityId',$Facility),@('@UnitTypeId',$UnitType))) {
  $p=$c.Parameters.Add($v[0],[System.Data.SqlDbType]::UniqueIdentifier);$p.Value=[Guid]$v[1]
 }
 foreach($v in @(@('@StartMonth',$Start),@('@EndMonth',$End))) {
  $p=$c.Parameters.Add($v[0],[System.Data.SqlDbType]::Date)
  if($null -eq $v[1]){$p.Value=[DBNull]::Value}else{$p.Value=[DateTime]::Parse($v[1])}
 }
 $p=$c.Parameters.Add('@NowUtc',[System.Data.SqlDbType]::DateTime2);$p.Scale=3;$p.Value=[DateTime]::Parse($Now)
 return $c
}
function Create($Start='2026-11-01',$End='2026-11-01',$Now='2026-10-11T00:00:00',$Customer=$taskCustomer,$Facility=$taskFacility,$UnitType=$taskUnitType) {
 $c=MakeCommand $taskConn $Start $End $Now $Customer $Facility $UnitType
 try{
  $r=$c.ExecuteReader()
  try {
   if(-not $r.Read()){throw 'Procedure returned no identifiers.'}
   return [pscustomobject]@{reservationId=$r.GetGuid($r.GetOrdinal('ReservationId'));invoiceId=$r.GetGuid($r.GetOrdinal('DepositInvoiceId'))}
  }finally{$r.Dispose()}
 }finally{$c.Dispose()}
}
function Reject([int]$Code,[string]$Name,$Start='2026-11-01',$End='2026-11-01',$Now='2026-10-11T00:00:00',$Customer=$taskCustomer,$Facility=$taskFacility,$UnitType=$taskUnitType) {
 $before=Snapshot
 $actual=0
 try{[void](Create $Start $End $Now $Customer $Facility $UnitType)}catch{
  $e=$_.Exception.GetBaseException()
  if($e.GetType().FullName -ne 'Microsoft.Data.SqlClient.SqlException'){throw}
  $actual=$e.Number
 }
 Check ($actual -eq $Code -and (Snapshot) -eq $before) $Name
}
try{
 if((Scalar 'SELECT DB_NAME()') -ne 'Frms_Test_CWP04'){throw 'Wrong test database.'}
 # Apply only the reviewed standalone procedure. Later EF migration adopts this exact body.
 $packet=Get-Content -LiteralPath 'C:\SWP391\backend\sql\20261011_FRMS_CreateReservation.sql' -Raw
 $packet=$packet.Replace('DECLARE @Apply bit=0;','DECLARE @Apply bit=1;').Replace('DECLARE @ExpectedDatabase sysname=NULL;', "DECLARE @ExpectedDatabase sysname=N'Frms_Test_CWP04';")
 ExecSql $packet
 $policy=Scalar "SELECT PolicyId FROM dbo.Policy WHERE Status='ACTIVE'"
 $timeout=Scalar "SELECT DepositTimeoutHours FROM dbo.Policy WHERE PolicyId='$policy'"
 ExecSql @"
INSERT dbo.UserAccount(UserAccountId,RoleId,Email,PhoneNumber,PasswordHash,Status,CreatedAt)
SELECT '$taskAccount',RoleId,'res001-$taskAccount@example.invalid','test-$($taskAccount.ToString('N').Substring(0,20))','test-only-not-for-login','ACTIVE',SYSUTCDATETIME() FROM dbo.UserRole WHERE RoleName='CUSTOMER';
INSERT dbo.Customer(CustomerId,UserAccountId,FullName) VALUES('$taskCustomer','$taskAccount',N'RES001 fixture');
INSERT dbo.Facility(FacilityId,Name,Address,Status) VALUES('$taskFacility',N'RES001 fixture',N'test','ACTIVE');
INSERT dbo.UnitType(UnitTypeId,Name,Mode,Size,RentalPrice) VALUES('$taskUnitType',N'RES001 fixture','PRIVATE',N'2m2',500000.25);
INSERT dbo.StorageUnit(StorageUnitId,FacilityId,UnitTypeId,UnitCode,Status) VALUES('$taskUnit','$taskFacility','$taskUnitType','TEST','AVAILABLE');
"@
 $created=Create '2026-11-01' '2027-01-01'
 $rid=$created.reservationId;$iid=$created.invoiceId
 Check ((Scalar "SELECT COUNT(*) FROM dbo.Reservation WHERE ReservationId='$rid' AND CustomerId='$taskCustomer' AND FacilityId='$taskFacility' AND UnitTypeId='$taskUnitType' AND PolicyId='$policy' AND LockedRentalPrice=500000.25 AND DepositAmount=500000.25 AND Status='PENDING_DEPOSIT' AND StartMonth='2026-11-01' AND EndMonth='2027-01-01' AND CreatedAt='2026-10-11T00:00:00'") -eq 1) 'DBT-RES001-01 snapshot and captured policy'
 Check ((Scalar "SELECT COUNT(*) FROM dbo.Invoice WHERE InvoiceId='$iid' AND EntityId='$rid' AND InvoiceType='DEPOSIT' AND BillingMonth IS NULL AND BaseAmount=500000.25 AND DiscountId IS NULL AND DiscountAmount=0 AND AmountDue=500000.25 AND Status='UNPAID' AND PaidAt IS NULL AND DueDate=DATEADD(HOUR,$timeout,CAST('2026-10-11T00:00:00' AS datetime2))") -eq 1) 'DBT-RES001-02 one correct unpaid deposit invoice'
 Check ((Scalar "SELECT COUNT(*) FROM dbo.AuditLog WHERE EntityId='$rid' AND UserAccountId='$taskAccount' AND Action='CREATE_RESERVATION' AND ISJSON(NewValue)=1") -eq 1 -and (Scalar "SELECT Status FROM dbo.StorageUnit WHERE StorageUnitId='$taskUnit'") -eq 'AVAILABLE' -and (Scalar "SELECT COUNT(*) FROM dbo.Contract WHERE ReservationId='$rid'") -eq 0 -and (Scalar "SELECT COUNT(*) FROM dbo.Visit WHERE EntityId='$rid'") -eq 0 -and (Scalar "SELECT COUNT(*) FROM dbo.Payment WHERE InvoiceId='$iid'") -eq 0) 'DBT-RES001-03 one audit; no physical assignment/Visit/Contract/Payment'
 ExecSql "UPDATE dbo.UnitType SET RentalPrice=999999 WHERE UnitTypeId='$taskUnitType';"
 Check ((Scalar "SELECT LockedRentalPrice FROM dbo.Reservation WHERE ReservationId='$rid'") -eq 500000.25) 'DBT-RES001-04 price snapshot remains unchanged'
 ClearCreated
 ExecSql "UPDATE dbo.UnitType SET RentalPrice=500000.25 WHERE UnitTypeId='$taskUnitType';"
 Reject 51101 'DBT-RES001-05 null start' $null
 Reject 51101 'DBT-RES001-06 null end' '2026-11-01' $null
 Reject 51101 'DBT-RES001-07 noncanonical start' '2026-11-02'
 Reject 51101 'DBT-RES001-08 noncanonical end' '2026-11-01' '2026-11-02'
 Reject 51101 'DBT-RES001-09 reversed range' '2026-12-01' '2026-11-01'
 Reject 51101 'DBT-RES001-10 current month' '2026-10-01' '2026-10-01'
 Reject 51101 'DBT-RES001-11 GMT7 month rollover' '2026-11-01' '2026-11-01' '2026-10-31T18:00:00'
 ExecSql "UPDATE dbo.UserAccount SET Status='INACTIVE' WHERE UserAccountId='$taskAccount';"
 Reject 51102 'SEC-RES001-12 inactive customer'
 ExecSql "UPDATE dbo.UserAccount SET Status='ACTIVE' WHERE UserAccountId='$taskAccount';"
 ExecSql "UPDATE dbo.UserAccount SET RoleId=(SELECT RoleId FROM dbo.UserRole WHERE RoleName='FACILITY_STAFF') WHERE UserAccountId='$taskAccount';"
 Reject 51102 'SEC-RES001-22 non-CUSTOMER role cannot create'
 ExecSql "UPDATE dbo.UserAccount SET RoleId=(SELECT RoleId FROM dbo.UserRole WHERE RoleName='CUSTOMER') WHERE UserAccountId='$taskAccount';"
 Reject 51102 'SEC-RES001-13 nonexistent customer' '2026-11-01' '2026-11-01' '2026-10-11T00:00:00' ([Guid]::NewGuid())
 ExecSql "UPDATE dbo.Facility SET Status='INACTIVE' WHERE FacilityId='$taskFacility';"
 Reject 51103 'DBT-RES001-14 inactive facility'
 ExecSql "UPDATE dbo.Facility SET Status='ACTIVE' WHERE FacilityId='$taskFacility';"
 Reject 51104 'DBT-RES001-15 missing unit type' '2026-11-01' '2026-11-01' '2026-10-11T00:00:00' $taskCustomer $taskFacility ([Guid]::NewGuid())
 ExecSql "UPDATE dbo.StorageUnit SET Status='MAINTENANCE' WHERE StorageUnitId='$taskUnit';"
 Reject 51106 'DBT-RES001-16 maintenance excluded from capacity'
 ExecSql "UPDATE dbo.StorageUnit SET Status='AVAILABLE' WHERE StorageUnitId='$taskUnit';"
 $hold=Create '2026-12-01' '2026-12-01'
 Reject 51106 'DBT-RES001-17 every month is checked; no partial creation' '2026-11-01' '2026-12-01'
 ClearCreated
 $hold=Create
 ExecSql "UPDATE dbo.Reservation SET Status='CANCELLED' WHERE ReservationId='$($hold.reservationId)';"
 $second=Create
 Check ((Scalar "SELECT COUNT(*) FROM dbo.Reservation WHERE CustomerId='$taskCustomer' AND Status='PENDING_DEPOSIT'") -eq 1) 'DBT-RES001-18 cancelled hold releases capacity'
 ClearCreated
 $occupied=Create
 ExecSql @"
UPDATE dbo.Reservation SET Status='COMPLETED' WHERE ReservationId='$($occupied.reservationId)';
UPDATE dbo.StorageUnit SET Status='IN_USE' WHERE StorageUnitId='$taskUnit';
INSERT dbo.Contract(ContractId,ReservationId,CustomerId,FacilityId,StorageUnitId,PolicyId,StartMonth,EndMonth,Status)
VALUES(NEWID(),'$($occupied.reservationId)','$taskCustomer','$taskFacility','$taskUnit','$policy','2026-11-01','2026-11-01','ACTIVE');
"@
 Reject 51106 'DBT-RES001-23 ACTIVE Contract occupancy counted, completed reservation not double-counted'
 ClearCreated
 ExecSql "UPDATE dbo.StorageUnit SET Status='AVAILABLE' WHERE StorageUnitId='$taskUnit';"
 $limit=Create '9999-11-01' '9999-12-01' '9999-10-11T00:00:00'
 Check ((Scalar "SELECT EndMonth FROM dbo.Reservation WHERE ReservationId='$($limit.reservationId)'") -eq [DateTime]'9999-12-01') 'DBT-RES001-24 maximum month does not overflow'
 ClearCreated
 # Controlled failure after Reservation insertion proves multi-table rollback.
 ExecSql "CREATE TRIGGER dbo.[$taskTrigger] ON dbo.Invoice AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.Reservation r ON r.ReservationId=i.EntityId WHERE r.FacilityId='$taskFacility') THROW 51999,'Fixture rollback proof',1; END;"
 Reject 51999 'DBT-RES001-19 invoice write failure rolls back reservation and audit'
 ExecSql "DROP TRIGGER dbo.[$taskTrigger];"
 # Simultaneous calls to one physical-capacity slot; all connections are independent.
 $pending=@()
 foreach($n in 1..4){
  $cx=[Microsoft.Data.SqlClient.SqlConnection]::new($taskBuilder.ConnectionString);$cx.Open()
  $cmd=MakeCommand $cx
  $pending+=@{connection=$cx;command=$cmd;task=$cmd.ExecuteReaderAsync()}
 }
 $wins=0;$capacityErrors=0
 foreach($call in $pending){
  try{$reader=$call.task.GetAwaiter().GetResult();try{if($reader.Read()){$wins++}}finally{$reader.Dispose()}}
  catch{$errorNumber=$_.Exception.GetBaseException().Number;if($errorNumber -eq 51106){$capacityErrors++}else{throw}}
  finally{$call.command.Dispose();$call.connection.Dispose()}
 }
 Check ($wins -eq 1 -and $capacityErrors -eq 3 -and (Scalar "SELECT COUNT(*) FROM dbo.Reservation WHERE CustomerId='$taskCustomer' AND Status='PENDING_DEPOSIT'") -eq 1) 'CON-RES001-20 exactly one winner; no overbooking'
 # Downgrade / redeploy only this known definition, preserving created history.
 $before=Snapshot
 ExecSql 'DROP PROCEDURE dbo.usp_CreateReservation;'
 ExecSql $packet
 Check ((Snapshot) -eq $before) 'DBT-RES001-21 procedure rollback/redeploy preserves history'
 Write-Output "Real SQL checks passed: $taskPassed; failed: 0"
}finally{
 if((Scalar "SELECT COUNT(*) FROM sys.triggers WHERE name='$taskTrigger'") -gt 0){ExecSql "DROP TRIGGER dbo.[$taskTrigger];"}
 ClearCreated
 ExecSql @"
DELETE dbo.StorageUnit WHERE StorageUnitId='$taskUnit';
DELETE dbo.UnitType WHERE UnitTypeId='$taskUnitType';
DELETE dbo.Facility WHERE FacilityId='$taskFacility';
DELETE dbo.Customer WHERE CustomerId='$taskCustomer';
DELETE dbo.UserAccount WHERE UserAccountId='$taskAccount';
"@
 $taskConn.Dispose()
}
