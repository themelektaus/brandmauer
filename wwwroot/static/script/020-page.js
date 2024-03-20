class Page
{
    static instances = { }
    static active = null
    
    static register(page)
    {
        page = new page
        this.instances[page.name] = page
    }
    
    #setup = false
    
    constructor()
    {
        this.name = this.constructor.name
        this.name = this.name.substr(0, this.name.length - 4).toLowerCase()
        this.$ = q(`[data-page="${this.name}"]`)
        this.#setDisplayNone(true)
    }
    
    async setActive(active)
    {
        for (const name in Page.instances)
            Page.instances[name].$.setClass(`fade-in`, false)
        
        this.#setDisplayNone(!active)
        
        if (!active)
        {
            if (Page.active == this)
                Page.active = null
            
            return
        }
        
        Page.active = this
        location.hash = this.name
        
        q(`.loading`).setClass(`display-none`, false)
        
        await Page.active.load()
        
        if (Page.active != this)
            return
        
        q(`.loading`).setClass(`display-none`, true)
        await delay(1)
        this.$.setClass(`fade-in`, true)
    }
    
    async load()
    {
        if (!this.#setup)
        {
            this.#setup = true
            await this.setup()
        }
        
        await this.refresh()
    }
    
    async setup()
    {
        InteractiveAction.checkDirtyBuild()
    }
    
    async refresh()
    {
        
    }
    
    #setDisplayNone(enabled)
    {
        this.$.setClass(`display-none`, enabled)
    }
}
