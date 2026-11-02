'use strict';
document.querySelectorAll('form[data-busy]').forEach(form => form.addEventListener('submit',() => {
    form.querySelector('.busy-box')?.classList.add('visible');
    form.querySelector('button[type=submit]')?.setAttribute('disabled','disabled');
}));
const pdfInput=document.querySelector('#pdf');
pdfInput?.addEventListener('change',()=>{
    const file=pdfInput.files[0];
    document.querySelector('#file-description').textContent=file ? file.name+' · '+(file.size/1024/1024).toFixed(2)+' MB' : '';
    pdfInput.setCustomValidity(file && file.size>20*1024*1024 ? 'File tối đa 20 MB.' : '');
});
document.querySelectorAll('form[data-ai]').forEach(form=>{
    let controller;
    const busy=form.querySelector('.busy-box');
    const submit=form.querySelector('button[type=submit]');
    form.querySelector('[data-cancel]')?.addEventListener('click',()=>controller?.abort());
    form.addEventListener('submit',async event=>{
        event.preventDefault();
        if(controller)return;
        controller=new AbortController();
        const timer=setTimeout(()=>controller?.abort(),90000);
        busy?.classList.add('visible'); submit.disabled=true;
        const phases=['Đang đọc slide…','Đang đối chiếu kiến thức…','Đang kiểm tra cấu trúc và nguồn…'];
        let phase=0;
        const phaseTimer=setInterval(()=>{const el=form.querySelector('[data-phase]');if(el)el.textContent=phases[Math.min(++phase,2)];},12000);
        try{
            const response=await fetch(form.action,{method:'POST',body:new FormData(form),signal:controller.signal,credentials:'same-origin'});
            if(response.redirected){location.assign(response.url);return;}
            if(response.ok){location.reload();return;}
            throw new Error('Yêu cầu chưa hoàn thành. Vui lòng tải lại trang và kiểm tra kết quả.');
        }catch(error){
            const message=document.createElement('p');message.setAttribute('role','alert');message.className='danger-text';
            message.textContent=error.name==='AbortError'?'Đã dừng chờ. Yêu cầu đang xử lý sẽ hủy nếu chưa hoàn tất; kiểm tra lại thư viện hoặc ví trước khi thử lại.':error.message;
            busy?.after(message);
        }finally{clearTimeout(timer);clearInterval(phaseTimer);controller=null;submit.disabled=false;busy?.classList.remove('visible');}
    });
});
document.querySelectorAll('[data-listen]').forEach(button=>button.addEventListener('click',()=>{
    if(!('speechSynthesis' in window)){button.textContent='Trình duyệt chưa hỗ trợ đọc';return;}
    if(speechSynthesis.speaking){speechSynthesis.cancel();button.textContent='▶ Nghe';return;}
    const message=document.querySelector(button.dataset.listen)?.textContent||'';
    const speech=new SpeechSynthesisUtterance(message);
    speech.lang=button.dataset.language==='auto'?( /[ăâđêôơưàảãạấầậếềệốồộớờợ]/i.test(message)?'vi-VN':'en-US'):button.dataset.language||'vi-VN';
    speech.onend=()=>button.textContent='▶ Nghe'; speech.onerror=()=>button.textContent='▶ Nghe';
    speechSynthesis.speak(speech);button.textContent='■ Dừng nghe';
}));
const game=document.querySelector('#sample-game');
if(game){
    const data=JSON.parse(game.dataset.scenario);let step=0,points=0;
    const choices=document.querySelector('#sample-choices'),next=document.querySelector('#sample-next'),outcome=document.querySelector('#sample-outcome');
    const render=()=>{
        const scene=data.scenes[step];
        document.querySelector('#sample-progress').textContent='Tình huống '+(step+1)+' / '+data.scenes.length;
        document.querySelector('#sample-topic').textContent=scene.topic;
        document.querySelector('#sample-message').textContent=scene.message;
        document.querySelector('#sample-source').textContent='Nguồn mẫu · slide '+scene.sourceSlide+': '+scene.sourceQuote;
        choices.replaceChildren();outcome.hidden=true;next.hidden=true;
        scene.choices.forEach(choice=>{
            const button=document.createElement('button');button.type='button';button.className='choice-button';
            button.textContent=choice.key+' · '+choice.text;
            button.addEventListener('click',()=>{
                choices.querySelectorAll('button').forEach(b=>b.disabled=true);
                points+=choice.quality==='good'?1:choice.quality==='mixed'?.5:0;
                outcome.className='outcome-card '+choice.quality;outcome.hidden=false;
                outcome.textContent=choice.outcome+' '+choice.why;next.hidden=false;
                next.textContent=step===data.scenes.length-1?'Xem tổng kết →':'Tình huống tiếp theo →';
            });choices.append(button);
        });
    };
    next.addEventListener('click',()=>{
        step++;
        if(step<data.scenes.length){render();return;}
        document.querySelector('#sample-progress').textContent='Bạn đã hoàn thành bản chơi thử.';
        document.querySelector('#sample-topic').textContent='Bài học đến từ việc đối chiếu nguồn.';
        document.querySelector('#sample-message').textContent='Điểm lựa chọn theo mẫu: '+points+' / '+data.scenes.length+'. Tạo tài khoản để dùng PDF riêng và lưu hành trình học tập.';
        choices.replaceChildren();outcome.hidden=true;next.hidden=true;
    });render();
}
document.querySelectorAll('form[data-confirm]').forEach(form=>form.addEventListener('submit',event=>{
    if(!confirm(form.dataset.confirm))event.preventDefault();
}));
window.addEventListener('pagehide',()=>{if('speechSynthesis' in window)speechSynthesis.cancel();});
