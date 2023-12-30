class AuditPage extends Page
{
    static _ = Page.register(this)
    
    #limit = 50
    #apiPath = `api/audit?limit=${this.#limit}`
    
    async refresh()
    {
        await super.refresh()
        
        this.model = await fetchJson(this.#apiPath)
        
        this.model.transferTo(this.$)
        
        this.$.qAll(`[data-object="selected"] [data-bind="status"]`).forEach($ =>
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
                
                default:
                    $.innerHTML = ``
                    break
            
            }
        })
        
        const $selected = this.$.q(`[data-object="selected"]`)
        $selected.scrollTop = $selected.scrollHeight
        
        this.$.qAll(`[data-bind="archive"] [data-bind="path"]`).forEach($ =>
        {
            const text = $.innerText
            $.innerText = `/${text}`
            $.onclick = async () =>
            {
                this.#apiPath = `${text}?limit=${this.#limit}`
                await this.refresh()
            }
        })
    }
}
