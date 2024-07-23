class ListPage extends Page
{
    constructor()
    {
        super()
        
        this.items = null
        
        this.$list = this.$.q(`[data-bind="${this.name}"]`)
        this.$itemTemplate = this.$list.q(`.item`)
        this.$list.removeChild(this.$itemTemplate)
        
        this.$.q(`[data-action="add"]`).onClick(async () =>
        {
            const model = await fetchJson(
                `api/${this.name}`,
                { method: 'POST' }
            )
            
            const item = ListPageItem.create(this, model)
            
            await this.createNode(item)
            
            item.model.transferTo(item.$)
            
            if (this.$search)
                this.$search.value = ``
            
            this.#onSearch()
            
            item.$simple.click()
        })
        
        this.$saveButton = this.$.q(`[data-action="saveAll"]`)
        this.$saveButton.onClick(async () =>
        {
            disable()
            
            this.$saveButton.setEnabled(false)
            this.$cancelButton.setEnabled(false)
            
            for (const item of this.items)
                await item.save()
            
            await InteractiveAction.checkDirtyBuild()
            
            this.items = null
            await this.refresh()
            
            enable()
        })
        
        this.$cancelButton = this.$.q(`[data-action="cancelAll"]`)
        this.$cancelButton.onClick(async () =>
        {
            disable()
            
            this.$saveButton.setEnabled(false)
            this.$cancelButton.setEnabled(false)
            
            this.items = null
            await this.refresh()
            
            enable()
        })
        
        this.$search = this.$.q(`.search`)
        if (this.$search)
            this.$search.on(`input`, () => this.#onSearch())
    }
    
    #onSearch()
    {
        if (!this.$search)
            return
        
        const search = this.$search.value.toLowerCase()
        
        this.items.forEach(x => x.$.setClass(`display-none`, true))
        
        const items = this.items.filter(x =>
        {
            const $$ = [
                x.$.q(`[data-bind="htmlName"]`),
                x.$.q(`[data-bind="htmlInfo"]`)
            ]
            
            for (const $ of $$)
            {
                if ($ && $.innerText.toLowerCase().includes(search))
                    return true
            }
            
            return false
        })
        
        items.forEach(x => x.$.setClass(`display-none`, false))
    }
    
    async setup()
    {
        await super.setup()
        
        setInterval(() =>
        {
            if (this.items == null)
                return
            
            let dirty = false
            
            for (const item of this.items)
            {
                const model = item.model.clone()
                item.$.transferTo(model)
                
                if (item.hashCode == model.getHashCode())
                    continue
                
                dirty = true
                break;
            }
            
            this.$saveButton.setEnabled(dirty)
            this.$cancelButton.setEnabled(dirty)
            
            Internal.setPageTargetDirty(this.name, dirty)
        }, 200)
    }
    
    async refresh()
    {
        await super.refresh()
        
        if (this.items == null)
        {
            await this.fetchData()
            await this.rebuildView()
        }
    }
    
    async fetchData()
    {
        this.items = []
        for (const model of await fetchJson(`api/${this.name}`))
            ListPageItem.create(this, model)
    }
    
    async rebuildView()
    {
        this.$list.innerHTML = ``
        
        for (const item of this.items)
        {
            await this.createNode(item)
            item.loadModelIntoView()
        }
        
        this.#onSearch()
    }
    
    async createNode(item)
    {
        item.createNode()
        this.$list.appendChild(item.$)
    }
}
