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
        this.$ = this.page.$itemTemplate.cloneNode(true)
        this.$.q(`[data-action="delete"]`).onClick(() => this.delete())
        
        this.$simple = this.$.children[0]
        this.$simple.onClick(() =>
        {
            for (const item of this.page.items)
                if (this.$details != item.$details)
                    item.$details.setClass(`display-none`, true)
            
            this.$details.classList.toggle(`display-none`)
        })
        
        this.$details = this.$.children[1]
        this.$details.setClass(`display-none`, true)
        
        this.page.$list.appendChild(this.$)
    }
    
    async save()
    {
        this.$.transferTo(this.model)
        
        const hashCode = this.model.getHashCode()
        if (this.hashCode == hashCode)
            return
        
        this.hashCode = hashCode
        
        await fetchPut(`api/${this.page.name}`, this.model)
        
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
