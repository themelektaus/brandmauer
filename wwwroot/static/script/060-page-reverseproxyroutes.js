class ReverseproxyroutesPage extends ListPage
{
    static _ = Page.register(this)
    
    async createNode(item)
    {
        await super.createNode(item)
        
        const id = item.model.identifier.id
        const $ = item.$.q(`.edit-script`)
        $.innerHTML = item.model.script ? `Edit Script` : `Create Script`
        $.href = `api/reverseproxyroutes/${id}?format=html`
    }
}
