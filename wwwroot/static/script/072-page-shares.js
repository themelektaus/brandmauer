class SharesPage extends ListPage
{
    static _ = Page.register(this)
    
    async fetchData()
    {
        await super.fetchData()
        
        for (const item of this.items)
        {
            item.model.password = atob(item.model.password)
            item.onSave = x => x.password = btoa(x.password)
        }
    }
}
