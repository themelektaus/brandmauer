class InteractivePasswordInput
{
    static _ = Interactive.register(this, () => qAll(`input[type="password"]`))
    
    static makeInteractive($)
    {
        $.setClass(`type-password`)
        
        const $parent = $.parentNode
        $parent.style.position = `relative`
        
        const $hidden = $parent.create(`div`)
        const $visible = create(`div`)
        
        const $hiddenGenerate = $hidden.create(`div`)
            .setHtml(`<i class="fa-solid fa-dice"></i>`)
        $hiddenGenerate.onclick = generate
        
        const $hiddenClear = $hidden.create(`div`)
            .setHtml(`<i class="fa-solid fa-eraser"></i>`)
        $hiddenClear.onclick = clear
        
        const $hiddenShow = $hidden
            .create(`div`)
            .setHtml(`<i class="fa-solid fa-eye"></i>`)
        $hiddenShow.onclick = show
        
        const $visibleGenerate = $visible.create(`div`)
            .setHtml(`<i class="fa-solid fa-dice"></i>`)
        $visibleGenerate.onclick = generate
        
        const $visibleHide = $visible
            .create(`div`)
            .setHtml(`<i class="fa-solid fa-eye-slash"></i>`)
        $visibleHide.onclick = hide
        
        $.addEventListener(`change`, refreshView)
        $.addEventListener(`input`, refreshView)
        
        refreshView()
        
        function show()
        {
            $.type = `text`
            $hidden.remove()
            $parent.appendChild($visible)
            getSelection().removeAllRanges()
        }
        
        function hide()
        {
            $.type = `password`
            $visible.remove()
            $parent.appendChild($hidden)
            getSelection().removeAllRanges()
        }
        
        function generate()
        {
            $.value = generatePassword()
            refreshView()
            show()
        }
        
        function clear()
        {
            $.value = ``
            refreshView()
        }
        
        function refreshView()
        {
            if (!$.value && !$visibleHide.classList.contains(`display-none`))
            {
                $visibleHide.setClass(`display-none`)
                $hiddenShow.setClass(`display-none`, false)
                hide()
            }
            
            if ($.dataset.generator == `disabled`)
            {
                $hiddenGenerate.setClass(`display-none`)
                $hiddenClear.setClass(`display-none`)
                $hiddenShow.setClass(`display-none`)
                $visibleGenerate.setClass(`display-none`)
                $visibleHide.setClass(`display-none`)
                return
            }
            
            $hiddenGenerate.setClass(`display-none`, $.value)
            $hiddenClear.setClass(`display-none`, !$.value)
            $hiddenShow.setClass(`display-none`, !$.value)
            $visibleGenerate.setClass(`display-none`, false)
            $visibleHide.setClass(`display-none`, !$.value)
        }
    }
}
