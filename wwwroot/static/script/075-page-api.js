class ApiPage extends Page
{
    static _ = Page.register(this)
    
    async refresh()
    {
        await super.refresh()
        
        this.model = await fetchJson(`api`)
        
        this.model.transferTo(this.$)
        
        this.$.qAll(`li`).forEach($ =>
        {
            
            const $method = $.q(`[data-bind="method"]`)
            const method = $method.dataset.value
            
            if (method)
            {
                $method.innerHTML = method
                
                const $path = $.q(`[data-bind="path"]`)
                const path = $path.dataset.value
                
                if (method == `GET` && !path.includes(`{`))
                {
                    $path.innerHTML = `<a href="${path}" target="_blank">/${path}</a>`
                }
                else
                {
                    $path.innerHTML = `/${path}`
                }
            }
        })
    }
}
