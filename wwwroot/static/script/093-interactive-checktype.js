class InteractiveCheckType
{
    static _ = Interactive.register(this, () => qAll(`[data-bind="checkType"]`))
    
    static makeInteractive($)
    {
        const $page = $.parentNode.parentNode
        
        const refreshView = () =>
        {
            $page.qAll(`.checktype-process`).forEach($x =>
            {
                $x.setClass(`display-none`, $.value != 1)
            })
            
            $page.qAll(`.checktype-service`).forEach($x =>
            {
                $x.setClass(`display-none`, $.value != 2)
            })
            
            $page.qAll(`.checktype-sqlconnection`).forEach($x =>
            {
                $x.setClass(`display-none`, $.value != 3)
            })
        }
        
        $.onChange(() => refreshView())
        
        refreshView()
    }
}
