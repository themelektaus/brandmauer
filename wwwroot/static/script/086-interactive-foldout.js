class InteractiveFoldout
{
    static _ = Interactive.register(this, () => qAll(`.foldout`))
    
    static makeInteractive($)
    {
        const $title = $.q(`.foldout-title`)
        const $content = $.q(`.foldout-content`)
        
        $content.setClass(`display-none`)
        $title.onClick(async () => $content.toggleClass(`display-none`))
    }
}
