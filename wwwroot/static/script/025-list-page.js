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
            item.createNode()
            
            for (const $ of item.$.qAll(`[data-options]`))
                await this.refreshOptions($)
            
            item.model.transferTo(item.$)
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
        
        this.items.forEach(x => x.$.setClass(`display-none`, true))
        
        const items = this.items.filter(x =>
        {
            const $htmlInfo = x.$.q(`[data-bind="htmlInfo"]`)
            if (!$htmlInfo)
                return false
            
            const text = $htmlInfo.innerText.toLowerCase()
            if (!text.includes(this.$search.value.toLowerCase()))
                return false
            
            return true
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
                const hashCode = model.getHashCode()
                if (item.hashCode == hashCode)
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
        
        if (this.items != null)
        {
            await this.refreshAllOptions()
            return
        }
        
        await this.fetchData()
        await this.rebuildView()
        this.loadModelIntoView()
        this.#onSearch()
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
            item.createNode()
        
        await this.refreshAllOptions()
    }
    
    async refreshAllOptions()
    {
        for (const $ of this.$.qAll(`[data-options]`))
        {
            await this.refreshOptions($)
            await delay(2)
        }
    }
    
    async refreshOptions($)
    {
        const value = $.value
        
        $.innerHTML = ``
        
        const page = Page.instances[$.dataset.options]
        
        $.create(`option`)
        
        await page.load()
        
        for (const item of page.items)
        {
            const $option = $.create(`option`)
            $option.value = item.model.identifier.id
            $option.text = item.model.shortName
        }
        
        $.value = value
    }
    
    loadModelIntoView()
    {
        for (const item of this.items)
        {
            item.model.transferTo(item.$)
            item.hashCode = item.model.getHashCode()
        }
    }
}
