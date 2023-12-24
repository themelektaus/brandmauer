class ApiPage extends Page
{
    static _ = Page.register(this)
    
    async refresh()
    {
        await super.refresh()
        
        this.model = await fetchJson(`api`)
        
        this.model.endpoints = this.model.endpoints
            .filter(x => x.method == "GET")
            .filter(x => !x.path.includes(`{id}`))
        
        this.model.transferTo(this.$)
        
        this.$.qAll(`[data-bind="path"]`).forEach($ =>
        {
            const text = $.innerText
            $.href = text
            $.innerText = `/${text}`
        })
    }
}
