class InteractivePort
{
    static _ = Interactive.register(this, () => qAll(`.port`))
    
    static makeInteractive($)
    {
        const $area = $.q(`[data-bind="area"]`)
        const $start = $.q(`[data-bind="start"]`)
        const $end = $.q(`[data-bind="end"]`)
        
        const refreshView = () =>
        {
            $start.parentNode.setClass(
                `display-none`,
                1 > $area.value || $area.value > 2
            )
            
            $end.parentNode.setClass(
                `display-none`,
                $area.value != 2
            )
            
            $start.previousElementSibling.setHtml(
                $area.value == 2 ? `Start` : `Port`
            )
        }
        
        $area.onChange(() => refreshView())
        
        refreshView()
    }
}
