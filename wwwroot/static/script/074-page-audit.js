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
        
        const $rows = this.$.qAll(`[data-bind="archive"] li.dynamic`)
        
        for (const $row of $rows)
        {
            $row.setClass(`display-none`, true)
        }
        
        const $status = this.$.qAll(`[data-object="selected"] [data-bind="status"]`)
        
        for (const $ of $status)
        {
            $.parentNode.setClass(`info`, true)
            $.innerHTML = `<i class="fas fa-circle-info"></i>`
        }
        
        for (const $row of $rows)
        {
            const $path = $row.q(`[data-bind="path"]`)
            
            if (!$path)
            {
                continue
            }
            
            const text = $path.innerHTML
            $path.innerHTML = `/${text}`
            $path.onClick(async () =>
            {
                this.#apiPath = `${text}?limit=${this.#limit}`
                await InteractiveAction.refreshPage()
            })
            
            await delay(1)
            
            $row.setClass(`display-none`, false)
        }
        
        await delay(1)
        
        for (const $ of $status)
        {
            const status = +$.innerText
            
            switch (status)
            {
                case 1:
                    $.parentNode.setClass(`warning`, true)
                    $.innerHTML = `<i class="fas fa-triangle-exclamation"></i>`
                    break
                    
                case 2:
                    $.parentNode.setClass(`error`, true)
                    $.innerHTML = `<i class="fas fa-circle-exclamation"></i>`
                    break
            }
            
            await delay(1)
        }
        
        const $selected = this.$.q(`[data-object="selected"]`)
        $selected.scrollTop = $selected.scrollHeight
        
        enable()
    }
}
