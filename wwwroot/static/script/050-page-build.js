class BuildPage extends Page
{
    static _ = Page.register(this)
    
    async setup()
    {
        await super.setup()
        
        if (LINUX)
        {
            this.$.q(`.linux-info`).remove()
        }
        else
        {
            this.$.q(`[data-action="apply"]`).setEnabled(false)
            this.$.q(`[data-action="clear"]`).setEnabled(false)
        }
    }
    
    async refresh()
    {
        await super.refresh()
        
        this.$.q(`pre`).innerText = await fetchText(`api/build/preview`)
    }
}
