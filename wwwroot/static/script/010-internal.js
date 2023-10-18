class Internal
{
    static transferModelToInput(model, $input, $done)
    {
        if ($done.has($input))
            return
        
        $done.add($input)
        
        const bind = $input.dataset.object || $input.dataset.bind
        
        if ($input.tagName == `UL`)
        {
            $input.qAll(`.dynamic`).forEach($ => $input.removeChild($))
            
            const $itemTemplate = $input.q(`li`).setClass(`display-none`)
            $itemTemplate.qAllBindings().forEach($ => $done.add($))
            
            const $add = $input
                .create(`button`)
                .setClass(`add`)
                .setClass(`dynamic`)
            
            const addItem = () =>
            {
                const $item = $itemTemplate.cloneNode(true)
                    .setClass(`display-none`, false)
                    .setClass(`dynamic`)
                
                $item.removeAttribute(`data-interactive`)
                $input.insertBefore($item, $add)
                
                $item.create(`button`)
                    .setClass(`remove`)
                    .onClick(() => $input.removeChild($item))
                
                return $item
            }
            
            $add.onClick(() => addItem())
            
            const list = model[bind]
            
            for (const item of list)
                item.transferTo(addItem(), $done)
            
            return
        }
        
        let value = this.getProperty(model, bind)
        
        if ($input.hasAttribute(`data-object`))
        {
            if (!value)
            {
                value = { id: 0 }
                this.setProperty(model, bind, value)
            }
            
            value.transferTo($input, $done)
            return
        }
        
        if ($input.type == `checkbox`)
        {
            $input.checked = value
            $input.dispatchEvent(new Event(`change`))
            return
        }
        
        if ($input.dataset.options && bind.split('.').slice(-1) != `id`)
        {
            if (!value)
                return
            
            value = value.id
        }
        
        if ([ `INPUT`, `SELECT`, `TEXTAREA` ].includes($input.tagName))
        {
            $input.value = value
            $input.dispatchEvent(new Event(`change`))
            return
        }
        
        if ($input.hasAttribute(`data-value`))
        {
            $input.dataset.value = value
            return
        }
        
        $input.innerHTML = value
    }
    
    static transferInputToModel($input, model, $done)
    {
        if ($done.has($input))
            return
        
        $done.add($input)
        
        const bind = $input.dataset.object || $input.dataset.bind
        
        if ($input.tagName == `UL`)
        {
            let i = 0
            const modelItems = this.getProperty(model, bind)
            const items = []
            $input.qAll(`li.dynamic`).forEach($ =>
            {
                const item = i < modelItems.length ? modelItems[i++] : { }
                $.transferTo(item, $done)
                if (item.id !== undefined && item.id === 0)
                    return
                items.push(item)
            })
            this.setProperty(model, bind, items)
            $input.qAllBindings().forEach($ => $done.add($))
            return
        }
        
        if ($input.hasAttribute(`data-object`))
        {
            const property = this.getProperty(model, bind);
            $input.transferTo(property, $done)
            return
        }
        
        if ($input.type == `number` || $input.dataset.type == `number`)
        {
            this.setProperty(model, bind, +$input.value)
            return
        }
        
        if ($input.type == `checkbox`)
        {
            this.setProperty(model, bind, $input.checked)
            return
        }
        
        if ([ `INPUT`, `SELECT`, `TEXTAREA` ].includes($input.tagName))
        {
            let value = $input.value
            
            if ($input.dataset.options && bind.split('.').slice(-1) != `id`)
                value = value ? { id: value } : null
            
            this.setProperty(model, bind, value)
            return
        }
    }
    
    static getProperty(model, bind)
    {
        const binds = bind.split('.')
        let property = model
        while (binds.length > 0)
            property = property[binds.shift()]
        return property
    }
    
    static setProperty(model, bind, value)
    {
        const binds = bind.split('.')
        let property = model
        while (binds.length > 1)
            property = property[binds.shift()]
        property[binds.shift()] = value
    }
    
    static getPageActions()
    {
        return qAll(`[data-action="gotoPage"]`)
    }
    
    static getPageTarget(name)
    {
        return q(`[data-action="gotoPage"][data-target="${name}"]`)
    }
    
    static setPageTargetDirty(name, dirty)
    {
        Internal.getPageTarget(name).setClass(`dirty`, dirty)
    }
}
