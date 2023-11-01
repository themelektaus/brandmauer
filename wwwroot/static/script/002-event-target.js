Object.defineProperties(EventTarget.prototype,
{
    create:
    {
        value: function()
        {
            const element = document.createElement(...arguments)
            if (this instanceof Node)
                this.appendChild(element)
            return element
        }
    },
    q:
    {
        value: function()
        {
            const $ = this instanceof Window ? document : this
            return $.querySelector(...arguments)
        }
    },
    qAll:
    {
        value: function()
        {
            const $ = this instanceof Window ? document : this
            return $.querySelectorAll(...arguments)
        }
    },
    qAllBindings:
    {
        value: function()
        {
            return this.qAll(`[data-bind], [data-object]`)
        }
    },
    setClass:
    {
        value: function(value, enabled)
        {
            if (enabled ?? true)
                this.classList.add(value)
            else
                this.classList.remove(value)
            return this
        }
    },
    toggleClass:
    {
        value: function(value)
        {
            this.classList.toggle(value)
            return this
        }
    },
    setHtml:
    {
        value: function(html)
        {
            this.innerHTML = html
            return this
        }
    },
    on:
    {
        value: function(selector, callback)
        {
            this.addEventListener(selector, e => callback(e.target, e))
            return this
        }
    },
    onClick:
    {
        value: function(callback)
        {
            return this.on(`click`, callback)
        }
    },
    onChange:
    {
        value: function(callback)
        {
            return this.on(`change`, callback)
        }
    },
    setEnabled:
    {
        value: function(enabled)
        {
            if (enabled ?? true)
                this.removeAttribute(`disabled`)
            else
                this.setAttribute(`disabled`, ``)
            return this
        }
    },
    isEnabled:
    {
        value: function()
        {
            const $ = this instanceof Window ? $body : this
            return !$.hasAttribute(`disabled`)
        }
    },
})
