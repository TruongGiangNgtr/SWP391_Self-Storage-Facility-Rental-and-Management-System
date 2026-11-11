import {expect,test} from '@playwright/test'
import {API,bearer,capturePosts,fixture,getAs,visitAs} from './batch12-helpers'
import {list,precondition,read} from './batch13-helpers'

test.setTimeout(120_000)
test.use({trace:'off'})

type Ticket={supportTicketId:string,contractId:string,category:string,description:string,status:string,resultNote:string|null,assignedEmployeeId:string|null,createdAt:string}
async function customerTicket(request:import('@playwright/test').APIRequestContext){
  const id=fixture('FRMS_TEST_CUSTOMER_2_SUPPORT_TICKET_ID')
  const t=await read<Ticket>(request,'CUSTOMER_2','/support-tickets/'+id)
  precondition(t.supportTicketId===id,'Customer 2 owned support ticket is valid')
  return t
}
async function ticketDetail(page:import('@playwright/test').Page,id:string){
  await visitAs(page,'CUSTOMER_2','/customer/support-tickets/'+id)
  await expect(page.getByRole('heading',{name:'Support Ticket Details'})).toBeVisible()
  return page.getByRole('region',{name:'Support ticket information'})
}

test('FRMS-ST-0127 - completed ticket shows Staff result without Close Ticket',async({request,page})=>{
  const ticket=await customerTicket(request)
  precondition(ticket.status==='COMPLETED'&&Boolean(ticket.resultNote),'owned COMPLETED ticket with recorded Staff result')
  const panel=await ticketDetail(page,ticket.supportTicketId)
  await expect(panel).toContainText(ticket.description,{timeout:45000})
  await expect(panel).toContainText(ticket.resultNote!)
  await expect(panel).toContainText(/Completed/i)
  await expect(page.getByRole('button',{name:'Close Ticket'})).toHaveCount(0)
  await expect(page.getByRole('button',{name:'Cancel Support Ticket'})).toHaveCount(0)
})
test('FRMS-ST-0173 - ticket detail displays real category status and linked contract',async({request,page})=>{
  const ticket=await customerTicket(request)
  const panel=await ticketDetail(page,ticket.supportTicketId)
  await expect(panel).toContainText(ticket.description,{timeout:45000})
  await expect(panel).toContainText('Staff Result')
  await expect(panel.getByRole('link',{name:'View Contract'}).first()).toHaveAttribute('href','/customer/contracts/'+ticket.contractId)
  const mayCancel=ticket.status==='OPEN'||ticket.status==='IN_PROGRESS'
  if(mayCancel)await expect(page.getByRole('button',{name:'Cancel Support Ticket'})).toBeVisible()
  else await expect(page.getByRole('button',{name:'Cancel Support Ticket'})).toHaveCount(0)
  await expect(page.getByRole('button',{name:'Close Ticket'})).toHaveCount(0)
})
test('FRMS-ST-0176 - out-of-catalogue ticket category cannot be submitted from select',async({request,page})=>{
  const contracts=await list<{contractId:string}>(request,'CUSTOMER','/contracts')
  precondition(contracts.length>0,'Customer 1 has an owned Contract for support form')
  await visitAs(page,'CUSTOMER','/customer/support-tickets/new')
  await expect(page).toHaveURL(/\/customer\/support-tickets\/new(?:[?#]|$)/,{timeout:45_000})
  await expect(page.getByRole('heading',{name:'Create Support Ticket',exact:true})).toBeVisible({timeout:45_000})
  const category=page.getByLabel('Category',{exact:true})
  await expect(category).toBeVisible({timeout:45000})
  const offered=await category.locator('option').evaluateAll(options=>options.map(x=>(x as HTMLOptionElement).value))
  precondition(offered.length>1,'supported catalogue options returned')
  expect(offered).not.toContain('NOT_A_VALID_CATEGORY')
  await expect(category).toHaveValue('')
  await page.getByLabel('Contract',{exact:true}).selectOption({index:1})
  await page.getByLabel('Description',{exact:true}).fill('Unrecognized ticket category validation')
  const posts=capturePosts(page,'/api/v1/support-tickets')
  await page.getByRole('button',{name:'Submit Support Ticket'}).click()
  await expect(page.getByText('Choose a category.',{exact:true})).toBeVisible()
  expect(posts()).toBe(0)
})
test('FRMS-ST-0180-B - F32 Staff cannot read F01 support ticket detail',async({request,page})=>{
  const id=fixture('FRMS_TEST_CUSTOMER_2_SUPPORT_TICKET_ID')
  // Positive facility/assignment control: F01 Staff can read this fixture first.
  const allowed=await getAs(request,'FACILITY_STAFF','/api/v1/support-tickets/'+id)
  precondition(allowed.status()===200,'F01 Staff has an accessible support ticket (not just an arbitrary ID)')
  const ticket=(await allowed.json() as {data?:Ticket}).data
  precondition(ticket?.supportTicketId===id,'positive F01 Staff Ticket read')
  const f32=await getAs(request,'FACILITY_STAFF_FOREIGN','/api/v1/support-tickets/'+id)
  expect([403,404],'different-facility Staff must not read F01 support ticket').toContain(f32.status())
  await visitAs(page,'FACILITY_STAFF_FOREIGN','/staff/support?ticketId='+id)
  await expect(page.getByRole('button',{name:'Complete Support Ticket'})).toHaveCount(0)
})
test('FRMS-ST-0145 - ACTIVE System Admin can open account controls',async({page})=>{
  await visitAs(page,'SYSTEM_ADMINISTRATOR','/admin/users')
  await expect(page.getByRole('heading',{name:'User account monitoring'})).toBeVisible({timeout:45000})
  const accounts=page.getByRole('table',{name:'User account monitoring'})
  await expect(accounts).toBeVisible()
  await expect(page.getByRole('region',{name:'Account list summary'})).toBeVisible()
  for(const field of ['User','Role','Contact','Status'])
    await expect(accounts.getByRole('columnheader',{name:field,exact:true})).toBeVisible()
})
test('FRMS-ST-0192 - authorized audit view shows actor action resource and timestamp safely',async({request,page})=>{
  const entries=await list<{auditLogId:string,action:string,entityType:string,createdAt:string}>(
    request,'SYSTEM_ADMINISTRATOR','/admin/audit-logs')
  precondition(entries.length>0,'actual audit log rows exist')
  await visitAs(page,'SYSTEM_ADMINISTRATOR','/admin/activity-logs')
  await expect(page.getByRole('heading',{name:'Activity log management'})).toBeVisible()
  const table=page.getByRole('table',{name:'Activity log history'})
  await expect(table).toBeVisible({timeout:45000})
  for(const column of ['Recorded (GMT+7)','Actor','Action','Business resource'])
    await expect(table.getByRole('columnheader',{name:column,exact:true})).toBeVisible()
  await table.getByRole('button',{name:/^View/}).first().click()
  await expect(page.getByRole('region',{name:'Audit event metadata'})).toBeVisible()
  const detail=page.getByRole('dialog',{name:'Activity detail'})
  await expect(detail).toBeVisible()
  const text=await detail.innerText()
  expect(text).not.toMatch(/passwordHash|connectionString|bearer\s+[a-z0-9._-]+|NpgsqlException/i)
})
