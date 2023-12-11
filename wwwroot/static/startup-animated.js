(function()
{
    function delay(ms) { return new Promise(x => setTimeout(x, ms)) }
    function show($) { if ($) { $.style.translate = 0; $.style.opacity = 1 } }
    (async () =>
    {
        await delay(50)
        show(q(`h1`))
        await delay(50)
        show(q(`.menu`))
        show(q(`main`))
    })()
})()