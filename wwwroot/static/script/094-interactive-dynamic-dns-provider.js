class InteractiveDynamicDnsProvider
{
    static _ = Interactive.register(this, () => qAll(`[data-bind="provider"]`))
    
    static makeInteractive($)
    {
        const $page = $.parentNode.parentNode
        
        const refreshView = () =>
        {
            $page.qAll(`.provider-noip`).forEach($x =>
            {
                $x.setClass(`display-none`, $.value != 1)
            })
            
            $page.qAll(`.provider-namecom`).forEach($x =>
            {
                $x.setClass(`display-none`, $.value != 2)
            })
        }
        
        $.onChange(() => refreshView())
        
        refreshView()
    }
}
