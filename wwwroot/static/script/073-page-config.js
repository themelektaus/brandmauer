class ConfigPage extends Page
{
    static _ = Page.register(this)
    
    constructor()
    {
        super()
        
        this.model = null
        
        this.$saveButton = this.$.q(`[data-action="saveConfig"]`)
        this.$cancelButton = this.$.q(`[data-action="cancelConfig"]`)
        
        this.$saveButton.onClick(async () =>
        {
            disable()
            this.$.transferTo(this.model)
            await fetchPut(`api/config`, this.model)
            await this.#reload()
            enable()
        })
        
        this.$cancelButton.onClick(async () =>
        {
            disable()
            await this.#reload()
            enable()
        })
    }
    
    async setup()
    {
        await super.setup()
        
        await this.#reload()
        
        setInterval(() =>
        {
            let dirty = false
            
            if (isEnabled())
            {
                const model = this.model.clone()
                this.$.transferTo(model)
                dirty = this.hashCode != model.getHashCode()
            }
            
            this.$saveButton.setEnabled(dirty)
            this.$cancelButton.setEnabled(dirty)
            
            Internal.setPageTargetDirty(`config`, dirty)
        }, 200)
    }
    
    async #reload()
    {
        this.model = await fetchJson(`api/config`)
        this.model.transferTo(this.$)
        
        this.hashCode = this.model.getHashCode()
    }
}
