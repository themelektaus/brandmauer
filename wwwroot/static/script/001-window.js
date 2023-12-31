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
        value: function()
        {
            $body.setEnabled(true)
        }
    },
    disable:
    {
        value: function()
        {
            $body.setEnabled(false)
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
