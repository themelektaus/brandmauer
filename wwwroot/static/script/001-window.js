Object.defineProperties(Window.prototype,
{
    $body:
    {
        get: function()
        {
            return document.body
        }
    },
    enable:
    {
        value: function(options)
        {
            window.screenMessages ??= []
            if (options?.screenMessage)
            {
                window.screenMessages.splice(
                    window.screenMessages.indexOf(options.screenMessage),
                    1
                )
            }
            
            q(`#fullscreen-loading`).setHtml(window.screenMessages.join(`<br>`))
            
            if (window.screenMessages.length)
                return
            
            $body.setEnabled(true)
            q(`#fullscreen-loading`).setClass(`display-none`, true)
        }
    },
    disable:
    {
        value: function(options)
        {
            $body.setEnabled(false)
            if (options?.screenMessage)
            {
                window.screenMessages ??= []
                if (window.screenMessages.indexOf(options.screenMessage) != -1)
                    return
                
                window.screenMessages.push(options.screenMessage)
                q(`#fullscreen-loading`)
                    .setClass(`display-none`, false)
                    .setHtml(window.screenMessages.join(`<br>`))
            }
        }
    },
    delay:
    {
        value: function(timeout)
        {
            return new Promise(x => setTimeout(x, timeout))
        }
    },
    log:
    {
        value: function()
        {
            console.log(...arguments)
        }
    },
    fetchText:
    {
        value: function()
        {
            return fetch(...arguments).then(x => x.text()).catch(() => { })
        }
    },
    fetchJson:
    {
        value: function()
        {
            return fetch(...arguments).then(x => x.json()).catch(() => { })
        }
    },
    fetchPut:
    {
        value: function(url, data)
        {
            return fetch(url, {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            })
        }
    },
})
