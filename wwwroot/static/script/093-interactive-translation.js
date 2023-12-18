class InteractiveTranslation
{
    static _ = Interactive.register(this, () => qAll('.translation'))
    
    static makeInteractive($)
    {
        const $protocol = $.q(`[data-bind="protocol"]`)
        const $sourcePort = $.q(`[data-bind="sourcePort"]`)
        const $i = $.q(`i`)
        const $targetPort = $.q(`[data-bind="targetPort"]`)
        
        const refreshView = () =>
        {
            const icmp = $protocol.value == 1
            $sourcePort.setClass(`invisible`, icmp)
            $i.setClass(`invisible`, icmp)
            $targetPort.setClass(`invisible`, icmp)
        }
        
        $protocol.onChange(() => refreshView())
        
        refreshView()
    }
}
