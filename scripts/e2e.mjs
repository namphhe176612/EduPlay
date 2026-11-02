import assert from 'node:assert/strict';
import { randomBytes } from 'node:crypto';
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';

const base=process.env.SABAN_TEST_URL||'http://localhost:5210';
const suffix=Date.now().toString(36)+randomBytes(3).toString('hex');
const prefix='qa-'+suffix;
const password=randomBytes(24).toString('base64url');
const emails=[prefix+'-one@example.test',prefix+'-two@example.test',prefix+'-admin@example.test'];
const sql=query=>execFileSync('sqlcmd',['-S','localhost','-E','-C','-d','GameDB','-b','-h','-1','-W','-Q','SET NOCOUNT ON; '+query],{encoding:'utf8'}).trim();
const quote=value=>"'"+String(value).replaceAll("'","''")+"'";
class Browser {
    cookies=new Map();
    async request(route,{method='GET',data,raw}={}) {
        const headers={Cookie:[...this.cookies].map(([k,v])=>k+'='+v).join('; ')};
        let body=raw;
        if(data){body=new URLSearchParams(data);headers['Content-Type']='application/x-www-form-urlencoded';}
        const res=await fetch(new URL(route,base),{method,headers,body,redirect:'manual'});
        for(const cookie of res.headers.getSetCookie()){
            const first=cookie.split(';')[0],index=first.indexOf('=');this.cookies.set(first.slice(0,index),first.slice(index+1));
        }
        return {res,body:await res.text()};
    }
    async page(route) {
        const page=await this.request(route);assert.equal(page.res.status,200,route+' should render');return page.body;
    }
    token(html){const match=html.match(/name="__RequestVerificationToken"[^>]*value="([^"]+)"/);assert.ok(match,'antiforgery token');return match[1];}
    async post(route,data,from='/Home/Dashboard') {
        const html=await this.page(from);
        return this.request(route,{method:'POST',data:{...data,__RequestVerificationToken:this.token(html)}});
    }
    async register(email) {
        const result=await this.post('/Account/Register',{FullName:'QA Learner '+suffix,Email:email,Password:password,Role:'2',Coin:'99999'},'/Account/Register');
        assert.equal(result.res.status,302,'registration redirect');await this.page('/Home/Dashboard');
    }
}
function makePdf(pages){
    const objects=[];
    const add=content=>(objects.push(content),objects.length);
    const catalog=add(''),tree=add(''),font=add('<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>');
    const kids=[];
    for(let i=0;i<pages;i++){
        const stream='BT /F1 18 Tf 60 760 Td (Marketing topic '+(i+1)+': customer segments and clear value propositions.) Tj ET';
        const content=add('<< /Length '+Buffer.byteLength(stream)+' >>\nstream\n'+stream+'\nendstream');
        const page=add('<< /Type /Page /Parent '+tree+' 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 '+font+' 0 R >> >> /Contents '+content+' 0 R >>');
        kids.push(page+' 0 R');
    }
    objects[catalog-1]='<< /Type /Catalog /Pages '+tree+' 0 R >>';objects[tree-1]='<< /Type /Pages /Kids ['+kids.join(' ')+'] /Count '+pages+' >>';
    let pdf='%PDF-1.4\n',offsets=[0];
    objects.forEach((object,i)=>{offsets.push(Buffer.byteLength(pdf));pdf+=(i+1)+' 0 obj\n'+object+'\nendobj\n';});
    const xref=Buffer.byteLength(pdf);pdf+='xref\n0 '+(objects.length+1)+'\n0000000000 65535 f \n';
    for(const offset of offsets.slice(1))pdf+=String(offset).padStart(10,'0')+' 00000 n \n';
    pdf+='trailer\n<< /Size '+(objects.length+1)+' /Root 1 0 R >>\nstartxref\n'+xref+'\n%%EOF\n';
    return Buffer.from(pdf);
}
const user=new Browser(),other=new Browser(),admin=new Browser();
const ids=[];
let passed=0;
function check(name){passed++;console.log('PASS '+name);}
async function upload(pages,unlock=false){
    const html=await user.page('/Documents/Upload'),form=new FormData();
    form.set('__RequestVerificationToken',user.token(html));form.set('language','vi');
    if(unlock)form.set('unlockPages','true');
    form.set('pdf',new Blob([makePdf(pages)],{type:'application/pdf'}),'qa-marketing.pdf');
    return user.request('/Documents/Upload',{method:'POST',raw:form});
}
try{
    await user.page('/');await user.page('/Home/Help');await user.page('/Home/Sample');check('public pages');
    await user.register(emails[0]);await other.register(emails[1]);
    ids.push(...sql('SELECT UserId FROM Users WHERE Email IN ('+emails.slice(0,2).map(quote).join(',')+')').split(/\s+/).filter(Boolean).map(Number));
    assert.equal(sql('SELECT CONCAT(Role,\':\',Coin) FROM Users WHERE Email='+quote(emails[0])),'1:20');check('registration hashes password, server role=1, 20 coins plus ledger');
    const badCsrf=await user.request('/Profile/Update',{method:'POST',data:{fullName:'Intruder'}});
    assert.equal(badCsrf.res.status,400);check('CSRF rejected');
    const uploaded=await upload(4);assert.equal(uploaded.res.status,302,'valid PDF upload');
    const fileId=Number(uploaded.res.headers.get('location').match(/\/(\d+)/)[1]);
    const filePage=await user.page('/Documents/Details/'+fileId);
    assert.match(filePage,/Marketing topic/);const pdf=await user.request('/Documents/Pdf/'+fileId);assert.equal(pdf.res.status,200);
    assert.equal((await other.request('/Documents/Details/'+fileId)).res.status,404);
    assert.equal((await other.request('/Documents/Pdf/'+fileId)).res.status,404);check('PDF extraction, original PDF, owner-only source');
    const tooMany=await upload(16);assert.equal(tooMany.res.status,200);
    assert.equal(sql('SELECT COUNT(*) FROM Files WHERE UserId='+ids[0]),'1');
    const unlock=await upload(16,true);assert.equal(unlock.res.status,302);
    assert.equal(sql('SELECT Coin FROM Users WHERE Email='+quote(emails[0])),'5');
    assert.equal(sql("SELECT COUNT(*) FROM CoinTransactions WHERE UserId="+ids[0]+" AND Amount=-15"),'1');check('PDF page unlock debits exactly 15 with ledger');
    const insufficient=await upload(16,true);assert.equal(insufficient.res.status,200);
    assert.equal(sql('SELECT COUNT(*) FROM Files WHERE UserId='+ids[0]),'2');check('insufficient PDF coins rollback metadata');
    const built=await user.post('/Scenarios/Build',{fileId,context:'QA scenario'},'/Documents/Details/'+fileId);
    assert.equal(built.res.status,302);
    const scenarioId=Number(built.res.headers.get('location').match(/\/(\d+)/)[1]);
    await user.page('/Scenarios/Details/'+scenarioId);
    assert.equal(sql('SELECT CoinCost FROM Scenarios WHERE ScenarioId='+scenarioId),'0');
    assert.equal(sql('SELECT Coin FROM Users WHERE UserId='+ids[0]),'5');check('demo build persists and is free');
    if(process.env.SABAN_TEST_LIVE_URL){
        const live=process.env.SABAN_TEST_LIVE_URL;
        const before=sql('SELECT Coin FROM Users WHERE UserId='+ids[0]);
        const beforeScenarios=sql('SELECT COUNT(*) FROM Scenarios WHERE UserId='+ids[0]);
        const failed=await user.post(live+'/Scenarios/Build',{fileId},live+'/Documents/Details/'+fileId);
        assert.equal(failed.res.status,302);
        assert.equal(sql('SELECT Coin FROM Users WHERE UserId='+ids[0]),before);
        assert.equal(sql('SELECT COUNT(*) FROM Scenarios WHERE UserId='+ids[0]),beforeScenarios);
        assert.equal(sql("SELECT COUNT(*) FROM AICalls WHERE UserId="+ids[0]+" AND IsSuccess=0 AND CoinCharged=0"),'1');
        check('Live provider configuration failure rolls back debit, ledger and scenario; logs failure');
    }
    const start=await user.post('/Scenarios/Start',{id:scenarioId},'/Scenarios/Details/'+scenarioId);
    assert.equal(start.res.status,302);const runId=Number(start.res.headers.get('location').match(/\/(\d+)/)[1]);
    for(let step=1;step<=4;step++){
        const route='/Scenarios/Play/'+runId;
        const selected=await user.post('/Scenarios/Choose',{id:runId,step,choice:['A','B','C','A'][step-1]},route);
        assert.equal(selected.res.status,302);
        await user.page(selected.res.headers.get('location'));
        if(step===1){
            await user.post('/Scenarios/Choose',{id:runId,step,choice:'C'},route);
            assert.equal(sql('SELECT COUNT(*) FROM RunChoices WHERE RunId='+runId),'1');check('repeat choice locked');
        }
    }
    const result=await user.page('/Scenarios/Results/'+runId);
    assert.match(result,/80/);assert.equal(sql('SELECT Status FROM Runs WHERE RunId='+runId),'Completed');
    assert.equal((await other.request('/Scenarios/Results/'+runId)).res.status,404);check('play to completion, server scoring, private results');
    await user.post('/Scenarios/Ask',{id:runId,question:'customer segments'},'/Scenarios/Results/'+runId);
    assert.equal(sql('SELECT COUNT(*) FROM ScenarioMessages WHERE ScenarioId='+scenarioId),'1');check('grounded demo answer persisted');
    await user.post('/Scenarios/Save',{id:runId},'/Scenarios/Results/'+runId);
    await user.post('/Scenarios/Save',{id:runId},'/Scenarios/Results/'+runId);
    assert.equal(sql('SELECT COUNT(*) FROM Saves WHERE RunId='+runId),'1');await user.page('/Profile/Vault');check('vault saved idempotently');
    await user.post('/Scenarios/Rate',{id:runId,rating:5},'/Scenarios/Results/'+runId);
    assert.equal(sql('SELECT Rating FROM Runs WHERE RunId='+runId),'5');check('rating persisted');
    const practice=await user.post('/Practice/Create',{scenarioId,count:3,focus:'customer'},'/Scenarios/Details/'+scenarioId);
    assert.equal(practice.res.status,302);const practiceId=Number(practice.res.headers.get('location').match(/\/(\d+)/)[1]);
    const questions=await user.page('/Practice/Play/'+practiceId);assert.ok(!questions.includes('"correct"'),'answers hidden before submit');
    await user.post('/Practice/Submit',{id:practiceId,'answers[0]':0,'answers[1]':1,'answers[2]':2},'/Practice/Play/'+practiceId);
    assert.equal(sql('SELECT CorrectAnswers FROM DeepPractices WHERE DeepPracticeId='+practiceId),'3');await user.page('/Practice/Play/'+practiceId);check('practice server grading and private answers');
    await user.page('/Wallet');await user.page('/Profile');check('wallet and profile render');
    execFileSync('dotnet',['bin/Debug/net10.0/PlayEdu.dll','--create-admin','true'],{
        cwd:process.cwd(),env:{...process.env,SABAN_ADMIN_EMAIL:emails[2],SABAN_ADMIN_PASSWORD:password},stdio:'pipe'
    });
    ids.push(Number(sql('SELECT UserId FROM Users WHERE Email='+quote(emails[2]))));
    const logged=await admin.post('/Account/Login',{Email:emails[2],Password:password},'/Account/Login');assert.equal(logged.res.status,302);
    await admin.page('/Admin');
    const forbidden=await admin.request('/Documents/Pdf/'+fileId);assert.equal(forbidden.res.status,302);assert.match(forbidden.res.headers.get('location'),/Denied/);check('admin aggregates render, cannot read learner PDF');
    await admin.post('/Admin/AdjustCoins',{id:ids[1],amount:5,reason:'QA adjustment'},'/Admin');
    assert.equal(sql('SELECT Coin FROM Users WHERE UserId='+ids[1]),'25');
    assert.equal(sql('SELECT COUNT(*) FROM AdminLogs WHERE AdminUserId='+ids[2]),'1');check('admin coin adjustment and audit atomic');
    await admin.post('/Admin/AdjustCoins',{id:ids[1],amount:-999,reason:'QA reject insufficient'},'/Admin');
    assert.equal(sql('SELECT Coin FROM Users WHERE UserId='+ids[1]),'25');check('negative balance blocked');
    await admin.post('/Admin/ToggleLock',{id:ids[1],reason:'QA lock'},'/Admin');
    const locked=await other.request('/Home/Dashboard');assert.equal(locked.res.status,302);check('locked session revoked');
    console.log('E2E completed: '+passed+' checks passed.');
}finally{
    const ownIds=sql('SELECT UserId FROM Users WHERE Email IN ('+emails.map(quote).join(',')+')').split(/\s+/).filter(Boolean).map(Number);
    if(ownIds.length){
        const owners=ownIds.join(',');
        sql("BEGIN TRAN; DELETE FROM Saves WHERE UserId IN ("+owners+"); DELETE FROM ScenarioMessages WHERE UserId IN ("+owners+"); "+
            "DELETE FROM RunChoices WHERE RunId IN (SELECT RunId FROM Runs WHERE UserId IN ("+owners+")); "+
            "DELETE FROM Runs WHERE UserId IN ("+owners+"); DELETE FROM DeepPractices WHERE UserId IN ("+owners+"); "+
            "DELETE FROM Scenarios WHERE UserId IN ("+owners+"); DELETE FROM Slides WHERE FileId IN (SELECT FileId FROM Files WHERE UserId IN ("+owners+")); "+
            "DELETE FROM Files WHERE UserId IN ("+owners+"); DELETE FROM CoinTransactions WHERE UserId IN ("+owners+"); "+
            "DELETE FROM AICalls WHERE UserId IN ("+owners+"); DELETE FROM AdminLogs WHERE AdminUserId IN ("+owners+") OR TargetUserId IN ("+owners+"); "+
            "DELETE FROM Users WHERE UserId IN ("+owners+"); COMMIT;");
        const root=path.resolve('App_Data/documents');
        for(const id of ownIds){assert.ok(Number.isSafeInteger(id)&&id>0);const directory=path.resolve(root,String(id));assert.equal(path.dirname(directory),root);fs.rmSync(directory,{recursive:true,force:true});}
    }
    console.log('Removed only this run’s QA records and PDF files.');
}


