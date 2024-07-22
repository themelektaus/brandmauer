class ReverseproxyroutesPage extends ListPage
{
    static _ = Page.register(this)
    
    async refresh()
    {
        await super.refresh()
        
        for (const item of this.items)
        {
            const id = item.model.identifier.id
            const $ = item.$.q(`.edit-script`)
            $.style.display = null
            $.innerHTML = item.model.script ? `Edit Script` : `Create Script`
            $.href = `api/reverseproxyroutes/${id}?format=html`
        }
    }
}
