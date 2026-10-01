// OrganizaJob: atalhos, colar/arrastar imagem, área de transferência e notificações.
window.oj = (() => {
    let ref = null;
    const modalAberto = () => document.querySelector('.modal') !== null;

    async function enviar(arquivo) {
        if (!ref || !arquivo) return;
        if (!arquivo.type.startsWith('image/')) {
            await ref.invokeMethodAsync('ImagemErro', 'Esse arquivo não é uma imagem.');
            return;
        }
        await ref.invokeMethodAsync('ImagemEnviando');
        try {
            const r = await fetch('api/imagens', { method: 'POST', headers: { 'Content-Type': arquivo.type }, body: arquivo });
            if (!r.ok) {
                await ref.invokeMethodAsync('ImagemErro', (await r.text()).replace(/^"|"$/g, '') || 'Falha ao enviar a imagem.');
                return;
            }
            const j = await r.json();
            await ref.invokeMethodAsync('ImagemRecebida', j.arquivo);
        } catch {
            await ref.invokeMethodAsync('ImagemErro', 'Falha ao enviar a imagem. O servidor está rodando?');
        }
    }

    function onKey(e) {
        const ctrl = e.ctrlKey || e.metaKey;
        const k = e.key.toLowerCase();
        if (ctrl && k === 'k') { e.preventDefault(); const b = document.getElementById('busca'); b?.focus(); b?.select(); }
        else if (ctrl && k === 'n') { e.preventDefault(); ref?.invokeMethodAsync('AtalhoNovo'); }
        else if (ctrl && e.key === 'Enter' && modalAberto()) { e.preventDefault(); ref?.invokeMethodAsync('AtalhoSalvar'); }
        else if (e.key === 'Escape' && modalAberto()) { e.preventDefault(); ref?.invokeMethodAsync('AtalhoEsc'); }
    }

    function onPaste(e) {
        const itens = [...(e.clipboardData?.items || [])];
        const img = itens.find(x => x.kind === 'file' && x.type.startsWith('image/'));
        if (!img) return;
        e.preventDefault();
        const arquivo = img.getAsFile();
        if (modalAberto()) { enviar(arquivo); return; }
        // fora do modal: abre um item novo e envia a imagem
        ref?.invokeMethodAsync('AtalhoNovo').then(() => setTimeout(() => enviar(arquivo), 50));
    }

    function onDragOver(e) {
        if (!modalAberto() || ![...(e.dataTransfer?.types || [])].includes('Files')) return;
        e.preventDefault();
        document.getElementById('oj-drop')?.classList.add('over');
    }
    function onDragLeave(e) {
        if (e.target?.id === 'oj-drop') e.target.classList.remove('over');
    }
    function onDrop(e) {
        if (!modalAberto()) return;
        const arquivo = e.dataTransfer?.files?.[0];
        if (!arquivo) return;
        e.preventDefault();
        document.getElementById('oj-drop')?.classList.remove('over');
        enviar(arquivo);
    }
    function onChange(e) {
        if (e.target?.id === 'oj-arquivo' && e.target.files?.[0]) {
            enviar(e.target.files[0]);
            e.target.value = '';
        }
    }

    return {
        init(dotnetRef) {
            ref = dotnetRef;
            document.addEventListener('keydown', onKey);
            document.addEventListener('paste', onPaste);
            document.addEventListener('dragover', onDragOver);
            document.addEventListener('dragleave', onDragLeave);
            document.addEventListener('drop', onDrop);
            document.addEventListener('change', onChange);
            return ('Notification' in window) ? Notification.permission : 'unsupported';
        },
        desligar() {
            ref = null;
            document.removeEventListener('keydown', onKey);
            document.removeEventListener('paste', onPaste);
            document.removeEventListener('dragover', onDragOver);
            document.removeEventListener('dragleave', onDragLeave);
            document.removeEventListener('drop', onDrop);
            document.removeEventListener('change', onChange);
        },
        async copiar(texto) {
            try {
                await navigator.clipboard.writeText(texto);
                return true;
            } catch {
                const ta = document.createElement('textarea');
                ta.value = texto;
                ta.style.position = 'fixed';
                ta.style.opacity = '0';
                document.body.appendChild(ta);
                ta.select();
                const ok = document.execCommand('copy');
                ta.remove();
                return ok;
            }
        },
        async pedirNotificacoes() {
            if (!('Notification' in window)) return 'unsupported';
            return await Notification.requestPermission();
        },
        notificar(titulo, corpo) {
            if ('Notification' in window && Notification.permission === 'granted') {
                const n = new Notification(titulo, { body: corpo || '', tag: titulo });
                n.onclick = () => { window.focus(); n.close(); };
            }
        }
    };
})();
