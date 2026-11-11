"""Self-tests do not touch the DB or load psycopg."""
import unittest
from pathlib import Path
import export_frms_profile as e

class ExportTests(unittest.TestCase):
    def test_no_pii_or_secrets(self):
        for c in ['passwordHash','JwtToken','Email','PhoneNumber','Cccd','FullName','Address','refreshToken','GatewayPayload','Note']:
            self.assertFalse(e.can_export('UserAccounts', c, 'text'),c)
        self.assertTrue(e.can_export('Reservations','ReservationId','uuid'))
        self.assertTrue(e.can_export('Visits','EntityId','uuid'))
        self.assertTrue(e.can_export('Visits','VisitDate','date'))
        self.assertTrue(e.can_export('StorageUnits','Status','text'))
        self.assertTrue(e.can_export('Facilities','Name','text'))
        self.assertFalse(e.can_export('Customers','Name','text'))
    def test_parse_connection_no_output(self):
        d=e.npgsql_to_libpq('Host=localhost;Port=5432;Database=frms;Username=dev;Password="x;y";SSL Mode=VerifyFull;Root Certificate=cert.crt',Path('/tmp/backend'))
        self.assertEqual(d['password'],'x;y')
        self.assertEqual(d['sslmode'],'verify-full')
        self.assertTrue(d['sslrootcert'].endswith('/backend/cert.crt'))
    def test_fixture_join(self):
        res={'ReservationId':'res1','FacilityId':e.F01,'UnitTypeId':'type1','Status':'CONFIRMED'}
        visit={'VisitId':'visit1','EntityId':'res1','VisitType':'RESERVATION','Status':'CHECKED_IN'}
        unit={'StorageUnitId':'unit1','FacilityId':e.F01,'UnitTypeId':'type1','Status':'AVAILABLE'}
        hints=e.infer_candidates({'Reservations':[res], 'Visits':[visit], 'StorageUnits':[unit]})
        self.assertEqual(hints['assignable_manager_f01']['candidates'][0]['storageUnitId'],'unit1')
        unit['Status']='IN_USE'
        hints=e.infer_candidates({'Reservations':[res], 'Visits':[visit], 'StorageUnits':[unit]})
        self.assertEqual(hints['assignable_manager_f01']['candidateCount'],0)

if __name__=='__main__':unittest.main()
