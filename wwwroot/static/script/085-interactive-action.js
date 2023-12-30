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
        Internal.getPageActions().forEach($ => $.setClass(`active`, false))
        
        const pages = Page.instances
        for (const page in Page.instances)
            await pages[page].setActive(false)
        
        let target = $sender
        if (typeof target != `string`)
            target = target.dataset.target
        
        Internal.getPageTarget(target).setClass(`active`, true)
        
        await pages[target].setActive(true)
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
        disable()
        const response = await fetch(`api/update/download`)
        await Page.active.refreshVersionInfo()
        enable()
    }
    
    static async updateInstall()
    {
        disable()
        
        const response = await fetch(`api/update/install`)
        debugLog(response)
        
        if (response.status != 200)
        {
            await Page.active.refreshVersionInfo()
            enable()
            return
        }
        
        await delay(5000)
        
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
    
    static async refreshPage($sender)
    {
        await Page.active.refresh()
    }
    
    static openSharedFile($sender)
    {
        const $description = $sender.parentNode.q(`[data-bind="description"]`)
        window.open(`share/${$description.value}`)
    }
}
