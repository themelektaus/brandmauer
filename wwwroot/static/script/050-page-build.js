class BuildPage extends Page
{
    static _ = Page.register(this)
    
    async refresh()
    {
        await super.refresh()
        
        this.$.q(`pre`).innerText = await fetchText(`api/build/preview`)
    }
}
