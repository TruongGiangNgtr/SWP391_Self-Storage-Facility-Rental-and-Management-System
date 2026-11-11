#!/usr/bin/env python3
"""Read-only FRMS PostgreSQL profile exporter. Never exports secret/PII values."""
from __future__ import annotations

import argparse
from datetime import date, datetime, time, timezone
from decimal import Decimal
import json
import os
from pathlib import Path
import re
import sys
import zipfile

F01 = 'f0000000-0000-0000-0000-000000000001'
F32 = 'f0000000-0000-0000-0000-000000000032'
ENV_KEYS = ('ConnectionStrings__Frms', 'ConnectionStrings__FrmsDb')
# No text bodies, arbitrary JSON, contact fields or authentication material is exported.
DENY_SUBSTRINGS = (
    'password','hash','salt','token','secret','credential','apikey','api_key',
    'privatekey','encryption','signature','otp','pin','session','cookie','jwt',
    'email','phone','mobile','cccd','identitynumber','passport','taxnumber',
    'address','street','postal','firstname','lastname','fullname','customername',
    'employee_name','personname','birth','gender','ipaddress','useragent',
    'payload','requestbody','responsebody','content','message','description',
    'comment','note','remark','reason','attachment','filename','filepath',
    'base64','binary','dataurl','webhook','callbackurl','imageurl','videourl',
    'document','photograph','evidencefile','bank','swift','iban','cardnumber',
    'cvv','cv2','accountnumber','externalreference','providertoken',
    'refresh','accesskey','clientsecret','authentication','authorization',
    'rawdata','metadata','oldvalue','newvalue','beforestate','afterstate',
    'sensitive','fax','deviceid','fingerprint','latitude','longitude',
)
DENY_TABLE_SUBSTRINGS = (
    'credential','token','secret','password','otp','session','authsession',
    'webhook','outbox','emailqueue','smsqueue','api_key','apikey',
    'verification','authentication','auditlog','accesslog',
)
SAFE_TEXT = {
    'status','state','role','type','visittype','worktype','unitcode',
    'unitstatus','inspectionstatus','reservationstatus','paymentstatus',
    'invoicekind','invoicetype','reasoncode','category','unitmode',
    'size','sortorder','facilitycode',
}
SAFE_NUMERIC = {
    'capacity','availablecapacity','totalcapacity','quantity','floor',
    'position','sequence','ordernumber','day','month','year','page','priority',
    'numberofunits', 'maxcapacity',
}
SAFE_BOOL = {'isactive','isdeleted','enabled','isavailable','isarchived'}
SAFE_TIMES = (
    'createdat','updatedat','deletedat','startdate','enddate','visitdate',
    'scheduleddate','duedate','actualreturndate','startmonth','endmonth',
    'completedat','cancelledat','confirmedat','checkedinat','checkedoutat',
    'expiresat','assignedat','releasedat','paidat','processedat',
)
SAFE_NAME_TABLES = (
    'facilit','unittype','damagetype','extrafeetype','policy','discount',
    'service','category','unit_size','unitsize',
)
EXCLUDED_TYPES = {'bytea', 'json', 'jsonb', 'xml', 'tsvector', 'tsquery'}


def normalized(s: str) -> str:
    return re.sub(r'[^a-z0-9]', '', s.lower())


def can_export(table: str, col: str, udt: str) -> bool:
    t, c = normalized(table), normalized(col)
    if udt.lower().lstrip('_') in EXCLUDED_TYPES:
        return False
    if any(term.replace('_','') in c for term in DENY_SUBSTRINGS):
        return False
    if c.endswith('id') and len(c) >= 3:
        return True
    if c in SAFE_TEXT or c in SAFE_NUMERIC or c in SAFE_BOOL:
        return True
    if c in SAFE_TIMES or (c.endswith('date') and not any(x in c for x in ('birth','identity','password','login'))):
        return True
    if c in ('name', 'displayname', 'title') and any(v in t for v in SAFE_NAME_TABLES):
        return True
    return False


def split_pairs(s: str) -> list[str]:
    parts, buf, quoted, escaped = [], [], None, False
    for ch in s:
        if escaped:
            buf.append(ch); escaped = False; continue
        if ch == '\\' and quoted:
            buf.append(ch); escaped = True; continue
        if ch in ('"', "'"):
            if quoted is None: quoted = ch
            elif quoted == ch: quoted = None
            buf.append(ch); continue
        if ch == ';' and quoted is None:
            if ''.join(buf).strip(): parts.append(''.join(buf).strip())
            buf = []
        else:
            buf.append(ch)
    if ''.join(buf).strip(): parts.append(''.join(buf).strip())
    return parts


def npgsql_to_libpq(value: str, backend_dir: Path) -> dict[str, str]:
    """Recognize the common Npgsql key/value connection string format."""
    mapping = {
        'host':'host','server':'host','port':'port','database':'dbname',
        'dbname':'dbname','username':'user','userid':'user','user':'user',
        'password':'password','sslmode':'sslmode',
        'rootcertificate':'sslrootcert','sslrootcert':'sslrootcert',
        'applicationname':'application_name','connecttimeout':'connect_timeout',
    }
    args: dict[str, str] = {}
    for part in split_pairs(value):
        if '=' not in part: continue
        k, v = part.split('=', 1)
        key = mapping.get(normalized(k))
        if key is None: continue
        v = v.strip()
        if len(v) >= 2 and v[0] == v[-1] and v[0] in ('"', "'"):
            v = v[1:-1]
        if key == 'sslmode':
            v = {'verifyca':'verify-ca','verifyfull':'verify-full',
                 'prefer':'prefer','require':'require','disable':'disable',
                 'allow':'allow'}.get(normalized(v), v.lower())
        if key == 'sslrootcert' and v and not Path(v).is_absolute():
            v = str((backend_dir / v).resolve())
        args[key] = v
    if not args.get('host') or not args.get('dbname') or not args.get('user'):
        raise ValueError('Incomplete PostgreSQL connection configuration (host/database/user required).')
    return args


def read_dotenv(f: Path) -> dict[str, str]:
    """Read only the two connection keys. Never print or save their values."""
    if not f.is_file(): return {}
    out: dict[str, str] = {}
    for line in f.read_text(encoding='utf-8-sig').splitlines():
        line = line.strip()
        if not line or line.startswith('#') or '=' not in line: continue
        k, v = line.split('=', 1)
        k = k.strip()
        if k not in ENV_KEYS: continue
        v = v.strip()
        if len(v) >= 2 and v[0] == v[-1] and v[0] in ('"', "'"):
            v = v[1:-1]
        out[k] = v
    return out


def locate_backend(start: Path) -> Path:
    for d in (start, *start.parents):
        candidate = d / 'backend' / 'Frms.Api'
        if candidate.is_dir(): return candidate
    raise FileNotFoundError('Cannot locate backend/Frms.Api; put this folder in repository root.')


def json_default(value):
    if isinstance(value, (datetime, date, time)):
        return value.isoformat()
    if isinstance(value, Decimal):
        return str(value)
    if isinstance(value, (bytes, bytearray)):
        return '[EXCLUDED_BINARY]'
    return str(value)


def obj_field(obj: dict, *choices):
    lookup = {normalized(str(k)): v for k, v in obj.items()}
    for key in choices:
        val = lookup.get(normalized(key))
        if val is not None: return str(val)
    return ''


def infer_candidates(entities: dict[str, list[dict]]) -> dict:
    rs, vs, us = [], [], []
    for table, records in entities.items():
        n = normalized(table)
        if 'reservation' in n and 'visit' not in n and 'assignment' not in n:
            rs.extend(row for row in records if obj_field(row,'reservationId'))
        elif 'visit' in n and 'reservation' not in n and 'work' not in n:
            vs.extend(row for row in records if obj_field(row,'visitId'))
        elif ('storageunit' in n or 'physicalunit' in n) and 'assignment' not in n:
            us.extend(row for row in records if obj_field(row,'storageUnitId', 'physicalUnitId'))
    visits_by_res: dict[str,list[dict]] = {}
    for row in vs:
        if obj_field(row,'visitType').upper() != 'RESERVATION':continue
        visits_by_res.setdefault(obj_field(row,'entityId','reservationId').lower(),[]).append(row)
    units_by_fac_type: dict[tuple[str,str],list[dict]] = {}
    for row in us:
        if obj_field(row, 'status').upper() != 'AVAILABLE':continue
        key = (obj_field(row,'facilityId').lower(), obj_field(row,'unitTypeId').lower())
        units_by_fac_type.setdefault(key,[]).append(row)
    assignable, foreign = [], []
    confirmed = checked_in = 0
    for row in rs:
        if obj_field(row,'status').upper() != 'CONFIRMED':continue
        confirmed += 1
        rid = obj_field(row,'reservationId')
        facility_id = obj_field(row,'facilityId')
        unit_type_id = obj_field(row,'unitTypeId')
        for visit in visits_by_res.get(rid.lower(),[]):
            visit_id = obj_field(visit,'visitId')
            if facility_id.lower() == F32.lower() and visit_id:
                foreign.append({'reservationId':rid,'visitId':visit_id,'facilityId':facility_id,'visitStatus':obj_field(visit,'status')})
            if obj_field(visit,'status').upper() != 'CHECKED_IN':continue
            checked_in += 1
            if facility_id.lower() != F01.lower():continue
            for unit in units_by_fac_type.get((facility_id.lower(), unit_type_id.lower()),[]):
                assignable.append({'reservationId':rid,'visitId':visit_id,
                                   'storageUnitId':obj_field(unit,'storageUnitId','physicalUnitId'),
                                   'facilityId':facility_id,'unitTypeId':unit_type_id})
    return {
        'assignable_manager_f01': {'candidateCount':len(assignable),'candidates':assignable[:100],
          'counts':{'confirmedReservations':confirmed,'checkedInReservationVisits':checked_in,
                    'availableStorageUnits':sum(len(v) for v in units_by_fac_type.values())},
          'requires': 'Manager F01 authorization and current availability; never perform assignment as a diagnostic.'},
        'cross_facility_visit_f32': {'candidateCount':len(foreign),'candidates':foreign[:100],
          'requires': 'Verify Staff F32 can read selected visit, Staff F01 cannot. Entity reference may be a Visit ID.'},
    }


def dump_db(args):
    try:
        import psycopg
        from psycopg import sql
    except ImportError:
        print('Missing dependency. Run: py -m pip install "psycopg[binary]"', file=sys.stderr)
        return 2

    backend_dir = locate_backend(Path(__file__).resolve().parent)
    env = read_dotenv(backend_dir / '.env')
    # The same precedence as ASP.NET Core: environment variable > .env, Frms > FrmsDb.
    source = (os.environ.get('ConnectionStrings__Frms') or env.get('ConnectionStrings__Frms')
              or os.environ.get('ConnectionStrings__FrmsDb') or env.get('ConnectionStrings__FrmsDb'))
    if not source:
        print('Missing ConnectionStrings__Frms / ConnectionStrings__FrmsDb in environment or backend .env.',file=sys.stderr)
        return 2
    try:
        conn_info = npgsql_to_libpq(source, backend_dir)
    except ValueError:
        print('Unsupported DB connection string. Check backend .env formatting locally; do not share its value.',file=sys.stderr)
        return 2

    outdir = (Path(args.out).expanduser().resolve() if args.out else Path.home() / 'FRMS_PROFILE_EXPORTS')
    outdir.mkdir(parents=True, exist_ok=True)
    stamp = datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%SZ')
    zip_path = outdir / f'FRMS_PROFILE_SAFE_{stamp}.zip'
    summary_path = outdir / f'FRMS_PROFILE_SUMMARY_{stamp}.json'
    print('FRMS read-only safe export | schema:', args.schema)
    print('Output directory:', outdir)
    print('Sensitive field values will NOT be exported. Database credentials will NOT be printed.')
    schemas, manifest = [], {'generatedAtUtc': datetime.now(timezone.utc).isoformat(),
        'schema':args.schema, 'mode':'read-only, allowlisted columns, all rows unless --row-limit is given',
        'tables':[], 'redaction':'PII/secrets/text/json excluded; IDs and selected state fields only',
        'rowLimitPerTable':args.row_limit}
    candidates_data = {}
    try:
        # Never use the production API or write business data. Read-only transaction at DB layer.
        conn_info.setdefault('connect_timeout', '15')
        with psycopg.connect(**conn_info) as conn:
            with conn.cursor() as cur:
                cur.execute('SET TRANSACTION READ ONLY')
                cur.execute("SELECT set_config('statement_timeout', %s, true)", (f'{args.timeout_s * 1000}ms',))
                cur.execute("""SELECT table_name FROM information_schema.tables
                    WHERE table_schema = %s AND table_type='BASE TABLE' ORDER BY table_name""", (args.schema,))
                tables = [r[0] for r in cur.fetchall()]
                cur.execute("""SELECT table_name, column_name, udt_name, data_type, ordinal_position
                   FROM information_schema.columns WHERE table_schema = %s
                   ORDER BY table_name, ordinal_position""", (args.schema,))
                cols_by_table = {}
                for t,c,u,dt,pos in cur.fetchall():
                    cols_by_table.setdefault(t,[]).append({'column':c,'pgType':u,'dataType':dt})
            with zipfile.ZipFile(zip_path, 'w', compression=zipfile.ZIP_DEFLATED, compresslevel=6, allowZip64=True) as zf:
                for t in tables:
                    cols = cols_by_table.get(t,[])
                    eligible = [c['column'] for c in cols if can_export(t,c['column'],c['pgType'])]
                    is_sensitive_table = any(x.replace('_','') in normalized(t) for x in DENY_TABLE_SUBSTRINGS)
                    entry = {'table':t,'safeColumns': [] if is_sensitive_table else eligible,
                             'excludedColumnCount':len(cols) if is_sensitive_table else len(cols)-len(eligible),
                             'exportedRows':0,'skipped':False}
                    schemas.append({'table':t, 'columns':cols})
                    if is_sensitive_table or not eligible:
                        entry['skipped']=True
                        entry['reason']='sensitive table or no safe columns'
                        manifest['tables'].append(entry)
                        continue
                    try:
                        with conn.cursor(name='frms_profile_cursor') as cur:
                            cur.itersize = 1000
                            stmt = sql.SQL('SELECT {} FROM {}.{}').format(
                                sql.SQL(',').join(sql.Identifier(c) for c in eligible),
                                sql.Identifier(args.schema), sql.Identifier(t))
                            if args.row_limit:
                                stmt += sql.SQL(' LIMIT {}').format(sql.Literal(args.row_limit))
                            cur.execute(stmt)
                            # Avoid traversal: archive member uses a numbered stable prefix.
                            member = f'data/{len(manifest["tables"])+1:04d}.jsonl'
                            with zf.open(member,'w',force_zip64=True) as output:
                                for record in cur:
                                    row={col: json_default(value) if not isinstance(value,(str,int,float,bool,type(None))) else value
                                         for col,value in zip(eligible, record)}
                                    output.write((json.dumps(row,ensure_ascii=False,separators=(',',':'),default=json_default)+'\n').encode('utf-8'))
                                    entry['exportedRows']+=1
                                    nt=normalized(t)
                                    if ('reservation' in nt or 'visit' in nt or 'storageunit' in nt or 'physicalunit' in nt):
                                        if len(candidates_data.get(t,[])) < 100000:
                                            candidates_data.setdefault(t,[]).append(row)
                            entry['file']=member
                    except psycopg.Error:
                        # Do not include DB exception payload, which can reveal host/database details.
                        conn.rollback()
                        # Re-establish read-only transaction after error.
                        with conn.cursor() as restore:
                            restore.execute('SET TRANSACTION READ ONLY')
                            restore.execute("SELECT set_config('statement_timeout', %s, true)", (f'{args.timeout_s * 1000}ms',))
                        entry['error']='query failed (permission, timeout or table changed)'
                        entry['skipped']=True
                    manifest['tables'].append(entry)
                    print(f"  {t}: {entry['exportedRows']} rows; {len(eligible)} safe columns" + (' [SKIPPED]' if entry['skipped'] else ''))
                hints=infer_candidates(candidates_data)
                zf.writestr('schema.json',json.dumps(schemas,indent=2,ensure_ascii=False))
                zf.writestr('manifest.json',json.dumps(manifest,indent=2,ensure_ascii=False))
                zf.writestr('fixture_candidates.json',json.dumps(hints,indent=2,ensure_ascii=False))
                zf.writestr('READ_ME_FIRST.txt',
                    'FRMS SANITIZED database test-profile snapshot. No raw .env/connection credentials, tokens, hashes, emails, phones, CCCDs, addresses, free text or JSON values are included.\n'
                    'ID values may still be linkable to people. Share only with a trusted reviewer.\n'
                    'Never commit this ZIP to Git. Export was read-only. Verify candidate ownership and state before use.\n')
            # transaction is only reads; with connection context will commit a read-only tx.
    except psycopg.Error:
        if zip_path.exists():zip_path.unlink()
        print('PostgreSQL connection/export failed. Inspect local DB access and TLS; do not send credentials.',file=sys.stderr)
        return 3
    except Exception as e:
        if zip_path.exists():zip_path.unlink()
        print(f'Export failed: {type(e).__name__}. Do not share secrets.', file=sys.stderr)
        return 4
    # Standalone summary for quick sharing; no raw personal fields.
    summary = {'manifest':manifest, 'fixtureCandidates':hints}
    summary_path.write_text(json.dumps(summary,indent=2,ensure_ascii=False),encoding='utf-8')
    print('\nDONE. Safe ZIP:',zip_path)
    print('Summary JSON:',summary_path)
    print('Candidate assignments:', len(hints['assignable_manager_f01']['candidates']))
    print('Foreign F32 visit candidates:',len(hints['cross_facility_visit_f32']['candidates']))
    print('No changes were made to PostgreSQL. Check contents before sharing.')
    return 0


def main():
    ap=argparse.ArgumentParser(description='FRMS read-only PostgreSQL sanitized fixture profile export')
    ap.add_argument('--schema',default='public',help='Postgres schema to export (default: public)')
    ap.add_argument('--out',default='',help='Output folder (default: home/FRMS_PROFILE_EXPORTS)')
    ap.add_argument('--row-limit',type=int,default=0,help='Optional maximum rows per table. 0=all rows (default).')
    ap.add_argument('--timeout-s',type=int,default=120,help='Statement timeout in seconds (default 120)')
    a=ap.parse_args()
    if not re.fullmatch(r'[A-Za-z_][A-Za-z_0-9]*',a.schema) or a.row_limit<0 or a.timeout_s<1:
        ap.error('invalid schema, row-limit, or timeout-s')
    return dump_db(a)

if __name__=='__main__':
    sys.exit(main())
