class InteractiveTimestamp
{
    static _ = Interactive.register(this, () => qAll(`[data-bind="timestamp"]`))
    
    static makeInteractive($)
    {
        const text = $.innerText
        const x = text.split(`T`)
        const y = x[0].split(`-`)
        const z = x[1]?.split(`.`)[0]
        $.innerText = `${y[2]}.${y[1]}.${y[0]} ${z}`
    }
}
