class Interactive
{
    static refreshing = false
    static instances = new Set
    
    static register(_class, $)
    {
        this.instances.add({ class: _class, $: $ })
    }
    
    static init()
    {
        new MutationObserver(() => this.#refresh())
            .observe($body, { subtree: true, childList: true, })
        
        this.#refresh()
    }
    
    static #refresh()
    {
        for (const instance of this.instances)
        {
            instance.$().forEach($ =>
            {
                if (!$.hasAttribute(`data-interactive`))
                {
                    $.dataset.interactive = ``
                    instance.class.makeInteractive($)
                }
            })
        }
    }
}
