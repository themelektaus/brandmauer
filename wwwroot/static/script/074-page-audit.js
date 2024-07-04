class AuditPage extends Page
{
    static _ = Page.register(this)
    
    #limit = 50
    #apiPath = `api/audit?limit=${this.#limit}`
    
    async refresh(loading)
    {
        if (loading)
        {
            await super.refresh()
            setTimeout(InteractiveAction.refreshPage, 2)
            return
        }
        
        disable()
        
        await super.refresh()
        
        this.model = await fetchJson(this.#apiPath)
        
        this.model.transferTo(this.$)
        
        const $status = this.$.qAll(`[data-object="selected"] [data-bind="status"]`)
        for (const $ of $status)
            $.style.visibility = `hidden`
        
        const $path = this.$.qAll(`[data-bind="archive"] [data-bind="path"]`)
        for (const $ of $path)
            $.style.visibility = `hidden`
        
        for (const $ of $status)
        {
            const status = +$.innerText
            
            switch (status)
            {
                case 0:
                    $.parentNode.setClass(`info`, true)
                    $.innerHTML = `<i class="fas fa-circle-info"></i>`
                    break
                    
                case 1:
                    $.parentNode.setClass(`warning`, true)
                    $.innerHTML = `<i class="fas fa-triangle-exclamation"></i>`
                    break
                    
                case 2:
                    $.parentNode.setClass(`error`, true)
                    $.innerHTML = `<i class="fas fa-circle-exclamation"></i>`
                    break
            }
            
            $.style.visibility = null
            
            await delay(1)
        }
        
        const $selected = this.$.q(`[data-object="selected"]`)
        $selected.scrollTop = $selected.scrollHeight
        
        for (const $ of $path)
        {
            const text = $.innerHTML
            $.innerHTML = `/${text}`
            $.onClick(async () =>
            {
                this.#apiPath = `${text}?limit=${this.#limit}`
                await InteractiveAction.refreshPage()
            })
            $.style.visibility = null
            await delay(1)
        }
        
        enable()
    }
}
