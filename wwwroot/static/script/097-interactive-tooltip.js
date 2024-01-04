class InteractiveTooltip
{
    static _ = Interactive.register(this, () => qAll(`[data-tooltip]`))
    
    static makeInteractive($)
    {
        const tooltip = $.dataset.tooltip
        const target = $.dataset.tooltipTarget
        const $target = target ? q(target) : $
        
        const $tooltip = $body
            .create(`div`)
            .setClass(`tooltip`, true)
            .setClass(`visible`, false)
            .setHtml(tooltip)
        
        const updatePosition = () =>
        {
            const targetRect = $target.getBoundingClientRect()
            const tooltipRect = $tooltip.getBoundingClientRect()
            const cw = (tooltipRect.width - targetRect.width) / 2
            $tooltip.style.left = `${targetRect.left - cw}px`
            $tooltip.style.top = `${targetRect.top - tooltipRect.height}px`
        }
        
        $.addEventListener(`mouseover`, () =>
        {
            $tooltip.setClass(`visible`, true)
            updatePosition()
        })
        
        $.addEventListener(`mouseout`, () =>
        {
            $tooltip.setClass(`visible`, false)
        })
    }
}
