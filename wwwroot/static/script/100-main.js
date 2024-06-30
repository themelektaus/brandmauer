
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
        on(`mousedown`, async ($, e) =>
        {
            if ($.getAttribute(`ripple`) == null)
                return
            
            const $ripple = create(`div`).setClass(`ripple`)
            $ripple.style.left = `${e.offsetX}px`
            $ripple.style.top = `${e.offsetY}px`
            $.insertBefore($ripple, $.firstChild)
            await delay(1)
            $ripple.setClass(`effect`)
            await delay(2000)
            $ripple.remove()
        })
        
        qAll(`li[data-action="gotoPage"][data-target="build"].display-none`).forEach(
            $ =>
            {
                if (!LINUX)
                {
                    $.remove()
                    return
                }
                
                $.setClass(`display-none`, false)
        })
        
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
