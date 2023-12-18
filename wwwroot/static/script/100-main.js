
Interactive.init()

const hash = location.hash

InteractiveAction.gotoPage(
    (hash && hash.length)
        ? hash.substring(1)
        : `dashboard`
)

{
    (function()
    {
        const $ = q(`li[data-action="gotoPage"][data-target="build"]`)
        
        if (WINDOWS)
        {
            $.remove()
            return
        }
        
        $.setClass(`display-none`, false)
        
    })()
}
