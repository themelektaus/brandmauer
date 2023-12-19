
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
        qAll(`li[data-action="gotoPage"][data-target].display-none`).forEach(
            $ =>
            {
                if (WINDOWS)
                {
                    $.remove()
                    return
                }
                
                $.setClass(`display-none`, false)
            }
        )
    })()
}
