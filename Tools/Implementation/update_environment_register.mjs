import fs from 'node:fs/promises';
import {Workbook} from '@oai/artifact-tool';
const root=new URL('../../',import.meta.url),folder=new URL('Docs/EnvironmentRevision/',root);
const path=new URL('Docs/ProductionFinish/VISUAL_ASSET_REGISTER.csv',root);
const wb=await Workbook.fromCSV(await fs.readFile(path,'utf8'),{sheetName:'Assets'});
const sheet=wb.worksheets.getItem('Assets'),rows=sheet.getUsedRange().values;
const original=structuredClone(rows),changed=new Set(['roads','parcel_edges','trees','sprite/tree']);
async function preview(name,data){
 const book=Workbook.create(),s=book.worksheets.add('Environment');
 const sample=[['Asset','Status','Integration','Evidence'],...data.map(r=>[r[0],r[6],r[8],r[9]])];
 const range=s.getRange(`A1:D${sample.length}`);range.values=sample;range.format.columnWidthPx=260;range.format.rowHeightPx=110;range.format.wrapText=true;
 s.getRange('A1:D1').format.fill='#174a46';s.getRange('A1:D1').format.font.color='#ffffff';book.recalculate();
 const png=await book.render({sheetName:'Environment',range:`A1:D${sample.length}`,scale:1,format:'png'});
 await fs.writeFile(new URL(name+'.png',folder),new Uint8Array(await png.arrayBuffer()));
}
if(process.argv.includes('--preview-only')){await preview('register_before',rows.filter(r=>changed.has(r[0])));process.exit(0);}
const receipt=JSON.parse(await fs.readFile(new URL('gameview_receipt.json',folder),'utf8'));
const art=JSON.parse(await fs.readFile(new URL('Assets/Weedtopia/Production/Environment/art_manifest.json',root),'utf8'));
rows[0][0]=String(rows[0][0]).replace('\uFEFF','');
for(const row of rows.slice(1))if(changed.has(row[0])){
 row[4]='Assets/Weedtopia/Production/Environment';row[5]='Docs/EnvironmentRevision/art_provenance.json';
 row[6]=row[0]==='sprite/tree'?'archived_source':'integrated_agent_reviewed';
 row[7]='Natural landscape Game View reviewed; final user acceptance open';
 row[8]=row[0]==='sprite/tree'?'Original asset retained; superseded by Woodland sprite prefabs':'Serialized natural landscape; existing buildings and route points preserved';
 row[9]='Docs/EnvironmentRevision/gameview_receipt.json';row[10]='Environment revision 2026-09-24; no release approval inferred';
}
const extra=art.sprites.map(s=>['environment/'+s.id,'environment_sprite','BurningHills','',`Assets/Weedtopia/Production/Environment/Art/${s.file}#${s.id}`,'Docs/EnvironmentRevision/art_provenance.json','integrated_agent_reviewed','Original generated pixels retained; final user acceptance open','Imported sprite and reusable serialized prefab','Docs/EnvironmentRevision/gameview_receipt.json',`Pivot bottom; world height ${s.worldHeight}; decorative objects do not intercept clicks`]);
const spare=extra.find(r=>r[0]==='environment/footbridge');spare[6]='prepared_reserve';spare[8]='Imported reusable prefab; not placed in current scene';spare[9]='Atlas reviewed; no active Game View instance';
for(const id of ['Meadow','GardenEarth','MossyFlagstones','ShadedMeadow','RiverWater'])extra.push(['environment/'+id,'environment_material','BurningHills','',`Assets/Weedtopia/Production/Environment/Materials/${id}.mat`,'Docs/EnvironmentRevision/art_provenance.json','integrated_agent_reviewed','Soft edges and restrained palette reviewed','Serialized mesh surfaces; authored shaders','Docs/EnvironmentRevision/gameview_receipt.json','Original Ground.png retained unchanged; material settings provide color grading']);
for(const row of extra){const index=rows.findIndex(r=>r[0]===row[0]);if(index>=0)rows[index]=row;else rows.push(row);}
for(let i=1;i<original.length;i++)if(!changed.has(original[i][0])&&!original[i][0].startsWith('environment/')&&JSON.stringify(rows[i])!==JSON.stringify(original[i]))throw Error('Unexpected row change: '+original[i][0]);
if(new Set(rows.slice(1).map(r=>r[0])).size!==rows.length-1)throw Error('Duplicate asset IDs');
sheet.getRange(`A1:K${rows.length}`).values=rows;wb.recalculate();
console.log((await wb.inspect({kind:'region',sheetId:'Assets',range:`A${rows.length-2}:K${rows.length}`,maxChars:1600,tableMaxRows:3,tableMaxCols:11})).ndjson);
await preview('register_after',[rows.find(r=>r[0]==='trees'),extra[0],extra[5],extra.at(-1)]);
const values=sheet.getRange(`A1:K${rows.length}`).values;
await fs.writeFile(path,values.map(r=>r.map(v=>'"'+String(v??'').replaceAll('"','""')+'"').join(',')).join('\r\n')+'\r\n');
await fs.writeFile(new URL('register_receipt.json',folder),JSON.stringify({utc:new Date().toISOString(),rows:rows.length-1,updatedExisting:4,environmentRows:21,unrelatedRowsPreserved:true,headerBomNormalized:true,method:'Artifact Tool CSV import, workbook edit, render and verified value export',gameviewFingerprint:receipt.fingerprint},null,2));
console.log('Environment register updated:',rows.length-1,'rows.');
