class InteractiveFoldout
{
    static _ = Interactive.register(this, () => qAll(`.foldout`))
    
    static makeInteractive($)
    {
        const $content = $.q(`.foldout-content`)
        $content.setClass(`display-none`)
        
        const $title = $.q(`.foldout-title`)
        $title.onClick(() =>
        {
            $content.toggleClass(`display-none`)
            if ($content.hasClass(`display-none`))
                return
            
            $content.scrollIntoView({ block: `nearest` })
        })
    }
}
