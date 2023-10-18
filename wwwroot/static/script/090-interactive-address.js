class InteractiveAddress
{
    static _ = Interactive.register(this, () => this.$addresses)
    
    static get $addresses()
    {
        return qAll(`[data-bind="addresses"]`)
    }
    
    static makeInteractive($)
    {
        $.onChange(async () => await this.resolve($))
    }
    
    static async resolve($)
    {
        if (!(Page.active instanceof HostsPage))
            return
        
        const $value = $.q(`li:nth-child(2) [data-bind="value"]`)
        
        if (!$value || !$value.value)
            return
        
        disable()
        const ip = await fetchText(`api/resolve?host=${$value.value}`)
        $.parentNode.parentNode.parentNode.qAll(`.ip`)
            .forEach($ => $.innerHTML = ip)
        enable()
    }
    
    static async resolveAll()
    {
        this.$addresses.forEach(this.resolve)
    }
}
