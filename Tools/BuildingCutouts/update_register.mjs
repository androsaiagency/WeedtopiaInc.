import fs from 'node:fs/promises';
import {Workbook} from 'file:///C:/Users/Mein-PC/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/@oai/artifact-tool/dist/artifact_tool.mjs';
const root=new URL('../../',import.meta.url);
const path=new URL('Docs/ProductionFinish/VISUAL_ASSET_REGISTER.csv',root);
const wb=await Workbook.fromCSV(await fs.readFile(path,'utf8'),{sheetName:'Assets'});
const sheet=wb.worksheets.getItem('Assets');
const rows=sheet.getUsedRange().values;
console.log((await wb.inspect({kind:'region',sheetId:'Assets',range:'G1:J3',maxChars:1300,tableMaxRows:3,tableMaxCols:4})).ndjson);
const manifest=JSON.parse(await fs.readFile(new URL('Docs/LayoutRevision/Cutouts/manifest.json',root),'utf8'));
const evidence=JSON.parse(await fs.readFile(new URL('Docs/LayoutRevision/gameview_receipt.json',root),'utf8'));
const slots=new Map(manifest.slots.map(x=>[x.key,x]));
async function preview(name){
  const p=Workbook.create();const s=p.worksheets.add('Register excerpt');
  s.getRange('A1:E4').values=rows.slice(0,4).map(r=>[r[0],r[6],r[7],r[8],r[9]]);
  s.getRange('A1:E4').format.columnWidthPx=220;s.getRange('A1:E4').format.rowHeightPx=110;s.getRange('A1:E4').format.wrapText=true;
  s.getRange('A1:E1').format.fill='#174a46';s.getRange('A1:E1').format.font.color='#ffffff';p.recalculate();
  const png=await p.render({sheetName:'Register excerpt',autoCrop:'all',scale:1,format:'png'});
  await fs.writeFile(new URL('Docs/LayoutRevision/'+name+'.png',root),new Uint8Array(await png.arrayBuffer()));
}
await preview('register_before');
if(process.argv.includes('--preview-only'))process.exit(0);
let updated=0;
for(let i=1;i<rows.length;i++){
  const s=slots.get(rows[i][0]);if(!s)continue;
  const sample='building_'+s.buildingId+'_'+s.tierId+'.png';
  const captured=evidence.screenshots.some(x=>x.endsWith(sample));
  rows[i][4]=s.sprite;
  rows[i][6]='approved_source_integrated';
  rows[i][7]='Individual contact-sheet review; source RGB preserved; final user acceptance open';
  rows[i][8]='Production wrapper integrated; seven original anchors and catalog preserved';
  rows[i][9]=captured?'Actual Game View: Docs/LayoutRevision/Screenshots/'+sample:'Imported and individually reviewed as cutout; family representative tiers captured in Game View';
  rows[i][10]='Four prototypes explicitly approved; local pixel-preserving cutout explicitly chosen; whole-game release not approved';
  updated++;
}
if(updated!==251||rows.length!==526)throw Error(`Unexpected register coverage: ${updated}/${rows.length}`);
sheet.getRange('A1:K526').values=rows;wb.recalculate();
await preview('register_after');
const output=sheet.getRange('A1:K526').values.map(r=>r.map(v=>'"'+String(v??'').replaceAll('"','""')+'"').join(',')).join('\r\n')+'\r\n';
await fs.writeFile(path,output);
await fs.writeFile(new URL('Docs/LayoutRevision/register_receipt.json',root),JSON.stringify({utc:new Date().toISOString(),rows:525,updated,method:'Artifact Tool CSV import, workbook edit and verified value export',gameviewFingerprint:evidence.fingerprint},null,2));
console.log('Updated 251 building rows; preserved 274 other rows.');
