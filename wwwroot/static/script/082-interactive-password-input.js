class InteractivePasswordInput
{
    static _ = Interactive.register(this, () => qAll(`input[type="password"]`))
    
    static makeInteractive($)
    {
        $.setClass(`type-password`)
        
        const $parent = $.parentNode
        $parent.style.position = `relative`
        
        const $buttons = $parent.create(`div`)
        
        const $show = $buttons
            .create(`div`)
            .setHtml(`<i class="fa-solid fa-eye"></i>`)
            .onClick(show)
        
        const $hide = $buttons
            .create(`div`)
            .setHtml(`<i class="fa-solid fa-eye-slash"></i>`)
            .onClick(hide)
        
        const $clear = $buttons
            .create(`div`)
            .setHtml(`<i class="fa-solid fa-eraser"></i>`)
            .onClick(clear)
        
        const $generate = $buttons
            .create(`div`)
            .setHtml(`<i class="fa-solid fa-dice"></i>`)
            .onClick(generate)
        
        $.addEventListener(`change`, refreshView)
        $.addEventListener(`input`, refreshView)
        
        refreshView()
        
        function show()
        {
            $.type = `text`
            getSelection().removeAllRanges()
            refreshView()
        }
        
        function hide()
        {
            $.type = `password`
            getSelection().removeAllRanges()
            refreshView()
        }
        
        function clear()
        {
            $.value = ``
            refreshView()
        }
        
        function generate()
        {
            $.value = generatePassword()
            show()
        }
        
        function refreshView()
        {
            const hasValue = $.value ? true : false
            const passwordVisible = $.type == `text`
            
            if (!hasValue && passwordVisible)
            {
                hide()
                return
            }
            
            if ($.dataset.generator == `disabled`)
            {
                $generate.setClass(`display-none`, true)
                $clear.setClass(`display-none`, true)
            }
            else
            {
                if ($.type == `text`)
                {
                    $generate.setClass(`display-none`, false)
                    $clear.setClass(`display-none`, true)
                }
                else
                {
                    $generate.setClass(`display-none`, hasValue)
                    $clear.setClass(`display-none`, !hasValue)
                }
            }
            
            $show.setClass(`display-none`, !hasValue || passwordVisible)
            $hide.setClass(`display-none`, !hasValue || !passwordVisible)
        }
    }
}
