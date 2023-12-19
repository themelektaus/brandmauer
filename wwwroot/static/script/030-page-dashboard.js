class DashboardPage extends Page
{
    static _ = Page.register(this)
    
    async refresh()
    {
        await super.refresh()
        
        this.model = await fetchJson(`api/info`)
        
        await this.refreshVersionInfo()
    }
    
    async refreshVersionInfo()
    {
        this.model.update = await fetchJson(`api/update`)
        
        const $installButton = this.$.q(`[data-action="updateInstall"]`)
        
        const version = this.model.update.downloadedVersion
        
        if (version.includes(`Not available`))
        {
            this.$.q(`[data-action="updateCheck"]`).setEnabled(false)
            this.$.q(`[data-action="updateDownload"]`).setEnabled(false)
            $installButton.setEnabled(false)
        }
        else if (version)
        {
            $installButton.setEnabled(true)
            Internal.setPageTargetDirty(`dashboard`, true)
        }
        else
        {
            $installButton.setEnabled(false)
            Internal.setPageTargetDirty(`dashboard`, false)
        }
        
        this.model.transferTo(this.$)
    }
}
