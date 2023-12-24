class InteractiveCheckbox
{
    static _ = Interactive.register(this, () => qAll(`input[type="checkbox"]`))
    
    static makeInteractive($)
    {
        const $checkbox = create(`div`).setClass(`checkbox`)
        
        $.parentNode.insertBefore($checkbox, $)
        
        $checkbox.appendChild($)
        
        $checkbox.create(`i`)
            .setClass(`fa-regular`, true)
            .setClass(`fa-square`, true)
            
        $checkbox.create(`i`)
            .setClass(`fa-solid`, true)
            .setClass(`fa-square-check`, true)
    }
}
