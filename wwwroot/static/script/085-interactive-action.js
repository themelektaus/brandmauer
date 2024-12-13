class InteractiveAction
{
    static _ = Interactive.register(this, () => qAll(`[data-action]`))
    
    static makeInteractive($)
    {
        $.onClick(async () =>
        {
            const action = this[$.dataset.action]
            if (action)
                await action($)
        })
    }
    
    static async gotoPage($sender)
    {
        disable()
        
        Internal.getPageActions().forEach($ => $.setClass(`active`, false))
        
        const pages = Page.instances
        for (const page in Page.instances)
            await pages[page].setActive(false)
        
        let target = $sender
        if (typeof target != `string`)
            target = target.dataset.target
        
        Internal.getPageTarget(target).setClass(`active`, true)
        
        await pages[target].setActive(true)
        
        enable()
    }
    
    static async apply()
    {
        disable()
        
        const result = await fetchJson(`api/build/apply`)
        log(result)
        
        await InteractiveAction.checkDirtyBuild()
        
        enable()
    }
    
    static async clear()
    {
        disable()
        
        const result = await fetchJson(`api/build/clear`)
        log(result)
        
        await InteractiveAction.checkDirtyBuild()
        
        enable()
    }
    
    static async checkDirtyBuild()
    {
        if (!LINUX)
            return
        
        const dirty = await fetchJson(`api/build/dirty`)
        Internal.setPageTargetDirty(`build`, dirty)
    }
    
    static async updateCheck()
    {
        disable()
        await Page.active.refreshVersionInfo()
        enable()
    }
    
    static async updateDownload()
    {
        const options = { screenMessage: `Downloading` }
        disable(options)
        const response = await fetch(`api/update/download`)
        await Page.active.refreshVersionInfo()
        enable(options)
    }
    
    static async updateInstall()
    {
        const options = { screenMessage: `Installing` }
        disable(options)
        
        const response = await fetch(`api/update/install`)
        
        if (response.status != 200)
        {
            await Page.active.refreshVersionInfo()
            enable(options)
            return
        }
        
        await delay(9000)
        
        enable(options)
        
        options.screenMessage = `Restarting`
        
        disable(options)
        
        for (let i = 0; i < 10; i++)
        {
            await delay(5000)
            
            const response = await fetch(``).catch(() => { })
            
            if (response && response.status == 200)
            {
                break
            }
        }
        
        location.reload()
    }
    
    static async updateCertificate($sender)
    {
        disable()
        
        const id = $sender.parentNode.dataset.value
        
        const letsEncrypt = $sender.dataset.letsEncrypt == "true"
        const staging = !($sender.dataset.staging == "false")
        
        const response = await fetch(
            `api/certificates/${id}/update` +
            `?letsEncrypt=${letsEncrypt}&staging=${staging}`
        )
        
        log(response)
        
        if (response.status == 200)
        {
            Page.active.items = null
            await Page.active.refresh()
        }
        
        enable()
    }
    
    static downloadCertificate($sender)
    {
        const id = $sender.parentNode.dataset.value
        const format = $sender.dataset.format
        window.open(`api/certificates/${id}/download?format=${format}`)
    }
    
    static async sendMail($sender)
    {
        disable()
        
        const $parent = $sender.parentNode
        const $form = $parent.parentNode
        
        const data = new URLSearchParams()
        data.append(`id`, $parent.dataset.value)
        data.append(`subject`, $form.q(`[data-key="subject"]`).value)
        data.append(`body`, $form.q(`[data-key="body"]`).value)
        data.append(`to`, $form.q(`[data-key="to"]`).value)
        
        const response = await fetch(`api/smtpconnections/send`,
        {
            method: `post`,
            headers: { "Content-Type": `application/x-www-form-urlencoded` },
            body: data
        })
        
        log(response.status)
        log(await response.text())
        
        enable()
    }
    
    static async runScript($sender)
    {
        disable()
        
        const $form = $sender.parentNode.parentNode
        const $result = $form.q(`.script-result`)
        $result.src = ``
        
        const response = await fetch(`api/run`,
        {
            method: `post`,
            headers: { "Content-Type": `text/plain` },
            body: $form.q(`[data-bind="script"]`).value
        })
        
        const content = await response.text()
        
        const $outputType = $form.q(`[data-bind="scriptOutputType"]`)
        
        if ($outputType.value == 2)
        {
            $result.src = `data:application/json;charset=utf-8,${escape(content)}`
        }
        else if ($outputType.value == 3)
        {
            $result.src = `data:text/html;charset=utf-8,${escape(content)}`
        }
        else
        {
            $result.src = `data:text/plain;charset=utf-8,${escape(content)}`
        }
        
        enable()
    }
    
    static async clearScript($sender)
    {
        $sender.parentNode.parentNode.q(`.script-result`).src = ``
    }
    
    static async refreshPage($sender)
    {
        disable()
        await Page.active.refresh()
        enable()
    }
    
    static openSharePage()
    {
        window.open(`share`)
    }
    
    static openShare1($sender)
    {
        const $token = $sender.parentNode.parentNode.q(`[data-bind="token"]`)
        window.open(`share/${$token.innerText}`)
    }
    
    static openShare2($sender)
    {
        const $parent = $sender.parentNode.parentNode
        const $token = $parent.q(`[data-bind="token"]`)
        const $password = $parent.q(`[data-bind="password"]`)
        window.open(`share/${$token.innerText}$${btoa($password.value)}`)
    }
}
