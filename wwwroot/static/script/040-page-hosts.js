class HostsPage extends ListPage
{
    static _ = Page.register(this)
    
    async refresh()
    {
        await super.refresh()
        
        await InteractiveAddress.resolveAll()
    }
}
