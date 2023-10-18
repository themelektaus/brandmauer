Object.defineProperties(Object.prototype,
{
    getHashCode:
    {
        value: function()
        {
            const json = JSON.stringify(this)
            let hash = 0
            for (let i = 0; i < json.length; i++)
                hash = (hash << 5) - hash + json.charCodeAt(i) | 0
            return hash
        }
    },
    transferTo:
    {
        value: function(target, $done)
        {
            $done ??= new Set
            
            if (this instanceof EventTarget)
            {
                this.qAllBindings().forEach($ =>
                {
                    Internal.transferInputToModel($, target, $done)
                })
                return
            }
            
            target.qAllBindings().forEach($ =>
            {
                Internal.transferModelToInput(this, $, $done)
            })
        }
    },
    clone:
    {
        value: function()
        {
            return JSON.parse(JSON.stringify(this))
        }
    },
})
