class InteractiveNameCom
{
    static key = `[data-object="provider_NameCom"]`
    
    static _ = Interactive.register(this, () => qAll(
        `${InteractiveNameCom.key} [data-bind="recordId"]`
    ))
    
    static makeInteractive($)
    {
        const $page = $.parentNode.parentNode
        
        const key = InteractiveNameCom.key
        const $username = $page.q(`${key} [data-bind="username"]`)
        const $password = $page.q(`${key} [data-bind="password"]`)
        const $domain = $page.q(`${key} [data-bind="domain"]`)
        const $record = $page.q(`${key} [data-bind="recordId"]`)
        
        const fetchDomains = async () =>
        {
            const username = $username.value
            if (!username)
                return
            
            const password = $password.value
            if (!password)
                return
            
            const options = { screenMessage: `Loading Domains of ${username}` }
            disable(options)
            
            const authentication = btoa(`${username}:${password}`)
            const domains = await fetchJson(`api/namecom/domains`,
            {
                headers: {
                    "Authorization": "Basic " + authentication
                }
            })
            
            const value = $domain.dataset.originValue || ``
            let html = `<option></option>`
            if (domains)
                for (const domain of domains)
                    html += `<option>${domain}</option>`
            $domain.innerHTML = html
            $domain.value = value
            
            enable(options)
            
            await fetchDomainRecords()
        }
        
        const fetchDomainRecords = async () =>
        {
            const username = $username.value
            if (!username)
                return
            
            const password = $password.value
            if (!password)
                return
            
            const domain = $domain.value
            if (!domain)
                return
            
            const options = { screenMessage: `Loading Records of ${domain}` }
            disable(options)
            
            const authentication = btoa(`${username}:${password}`)
            const records = await fetchJson(
                `api/namecom/domains/${domain}/records`,
                {
                    headers: {
                        "Authorization": "Basic " + authentication
                    }
                }
            )
            
            const value = +($record.dataset.originValue || "0")
            let html = `<option value="0"></option>`
            if (records)
                for (const record of records)
                    html += `<option value="${record.id}">${record.fqdn}</option>`
            $record.innerHTML = html
            $record.value = value
            
            enable(options)
        }
        
        $username.onChange(async () => await fetchDomains())
        $password.onChange(async () => await fetchDomains())
        $domain.onChange(async () => await fetchDomainRecords())
    }
}
