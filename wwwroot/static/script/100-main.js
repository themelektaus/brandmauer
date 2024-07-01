{
    (function()
    {
        
        if (!LINUX)
        {
            qAll(`[data-target-page="build"]`).forEach($ => $.remove())
        }
        
        if (WINDOWS)
        {
            qAll(`[data-target-page="services"]`).forEach($ => $.remove())
            qAll(`[data-target-page="rules"]`).forEach($ => $.remove())
            qAll(`[data-target-page="natroutes"]`).forEach($ => $.remove())
        }
        
    })()
}

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
        
    })()
}

{
    (function()
    {
        function getCssFloat(name)
        {
            const style = getComputedStyle(document.documentElement)
            return parseFloat(style.getPropertyValue(name))
        }
        
        class Menu
        {
            #menu
            #bar
            #indicator
            #items = []
            
            constructor()
            {
                this.#menu = q(`.menu`)
                this.#bar = this.#menu.q(`.menu-bar`)
                
                on("mouseup", () =>
                {
                    this.#bar.qAll(".icon").forEach($ => $.setClass("active", false))
                })
                
                this.#indicator = this.#menu.q(`.menu-indicator`)
                
                if (!this.#bar.children.length)
                    return
                
                for (const item of this.#bar.children)
                {
                    this.#items.push({
                        label: item.dataset.label,
                        icon: item.dataset.icon,
                        targetPage: item.dataset.targetPage,
                        active: item.getAttribute(`active`) !== null
                    })
                }
                
                this.#bar.innerHTML = ``
                
                for (const item of this.#items)
                {
                    const $ = this.#bar.create(`div`).setClass(`menu-bar-item`)
                    const html = `<i class="fa fa-${item.icon}"></i>`
                    $.dataset.action = `gotoPage`
                    $.dataset.target = item.targetPage
                    $.create(`div`).setClass(`icon`).setHtml(html)
                    $.create(`div`).setClass(`label`).setHtml(item.label)
                    $.create(`div`).setClass(`hover-label`).setHtml(item.label)
                }
                
                const items = [...this.#bar.children]
                for (const i in items)
                    this.#setupBarItem(items, items[i], i)
                
                setTimeout(() =>
                {
                    this.#indicator.style.transition = `none`
                    
                    for (const i in items)
                        if (items[i].hasClass(`active`))
                            this.#setIndicatorOffset(i)
                    
                    setTimeout(() =>
                    {
                        this.#indicator.setClass(`display-none`, false)
                        this.#indicator.style.transition = null
                    })
                })
            }
            
            #setupBarItem(items, item, i)
            {
                item.on(`mousedown`, () => item.q(".icon").setClass(`active`))
                
                const activate = () =>
                {
                    if (item.hasClass(`active`))
                        return
                    
                    items.forEach((item, j) => item.setClass(`active`, i == j))
                    
                    this.#setIndicatorOffset(i)
                }
                
                item.on(`click`, activate)
                
                if (this.#items[i].active)
                    activate()
            }
            
            #setIndicatorOffset(i)
            {
                const l = this.#bar.children.length
                
                let s = getCssFloat("--menu-item-size")
                s += getCssFloat("--menu-item-spacing")
                
                let offset = Math.ceil(l / 2) * s - s / 2
                
                if (l % 2 == 1)
                    offset -= s / 2
                
                offset -= i * s
                
                this.#indicator.style.transform = `translateX(${-offset}rem)`
            }
        }
        
        new Menu
        
    })()
}
