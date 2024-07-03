class ListPageItem
{
    static create(page, model)
    {
        const item = new ListPageItem
        
        item.page = page
        item.page.items.push(item)
        
        item.model = model
        item.hashCode = item.model.getHashCode()
        
        return item
    }
    
    get index()
    {
        return this.page.items.indexOf(this)
    }
    
    createNode()
    {
        let detailsLoaded = false
        
        this.$ = this.page.$itemTemplate.cloneNode(true)
        this.$.q(`[data-action="delete"]`).onClick(() => this.delete())
        
        this.$simple = this.$.children[0]
        this.$simple.onClick(async () =>
        {
            for (const item of this.page.items)
            {
                if (this.$details != item.$details)
                {
                    item.$details.setClass(`display-none`, true)
                }
            }
            
            this.$details.toggleClass(`display-none`)
            
            if (!this.$details.hasClass(`display-none`))
            {
                if (!detailsLoaded)
                {
                    detailsLoaded = true
                    await this.refreshOptions()
                }
                
                this.$.scrollIntoView({ block: `nearest` })
            }
        })
        
        this.$details = this.$.children[1]
        this.$details.setClass(`display-none`, true)
    }
    
    async refreshOptions()
    {
        disable()
        
        for (const $ of this.$.qAll(`[data-options]`))
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
        
        this.loadModelIntoView()
        
        enable()
    }
    
    loadModelIntoView()
    {
        this.model.transferTo(this.$)
        this.hashCode = this.model.getHashCode()
    }
    
    async save()
    {
        this.$.transferTo(this.model)
        
        const hashCode = this.model.getHashCode()
        if (this.hashCode == hashCode)
            return
        
        this.hashCode = hashCode
        
        let model = this.model
        
        if (this.onSave)
        {
            model = model.clone()
            this.onSave(model)
        }
        
        await fetchPut(`api/${this.page.name}`, model)
        
        log(`[OK] ${this.model.identifier.id}`)
    }
    
    async delete()
    {
        await fetch(`api/${this.page.name}/${this.model.identifier.id}`, {
            method: 'DELETE'
        })
        
        this.page.items.splice(this.index, 1)
        this.page.$list.removeChild(this.$)
    }
}
