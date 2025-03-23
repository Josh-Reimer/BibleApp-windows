import sys
import wx
import pyperclip
import ChapterVerse as CV
import list_and_str_ops as list
from BibleFileNames import files
from charEliminator import onlyVerses, remove_only_chars

class BibleApp(wx.Frame):
    """
    A simple desktop Bible search application using wxPython.
    Allows users to enter search keywords and retrieve relevant verses
    from Bible text files.
    """
    def __init__(self, *args, **kw):
        super(BibleApp, self).__init__(*args, **kw)
        self.InitUI()

    def InitUI(self):
        panel = wx.Panel(self)
        vbox = wx.BoxSizer(wx.VERTICAL)

        hbox1 = wx.BoxSizer(wx.HORIZONTAL)
        lbl = wx.StaticText(panel, label='Enter search keywords: ')
        self.search_input = wx.TextCtrl(panel, size=(300, -1))
        search_btn = wx.Button(panel, label='SEARCH')
        search_btn.Bind(wx.EVT_BUTTON, self.OnSearch)
        clear_btn = wx.Button(panel, label='Clear')
        clear_btn.Bind(wx.EVT_BUTTON, self.OnClear)
        
        hbox1.Add(lbl, flag=wx.RIGHT, border=8)
        hbox1.Add(self.search_input, flag=wx.RIGHT, border=8)
        hbox1.Add(search_btn, flag=wx.RIGHT, border=8)
        hbox1.Add(clear_btn, flag=wx.RIGHT, border=8)
        vbox.Add(hbox1, flag=wx.EXPAND|wx.ALL, border=10)
        
        self.result_area = wx.TextCtrl(panel, style=wx.TE_MULTILINE|wx.TE_READONLY, size=(600, 400))
        vbox.Add(self.result_area, proportion=1, flag=wx.EXPAND|wx.ALL, border=10)
        
        panel.SetSizer(vbox)
        self.SetTitle('Desktop Bible')
        self.SetSize((800, 600))
        self.Centre()
    
    def OnSearch(self, event):
        searchkey = self.search_input.GetValue()
        results = self.linear_search(searchkey)
        self.result_area.SetValue(results)
    
    def OnClear(self, event):
        self.search_input.SetValue('')
        self.result_area.SetValue('')
    
    def linear_search(self, searchkey):
        multiple_results = []
        for book in files:
            with open(book, 'r') as f:
                verses = f.readlines()
            book_cleaned = self.clean(book)
            for verse in verses:
                if searchkey.lower() in verse.lower():
                    multiple_results.append(f"{book_cleaned}\n{verse}")
        return '\n'.join(multiple_results) if multiple_results else "No results found."

    def clean(self, text):
        """
        Cleans up the book name by:
        - Replacing underscores with spaces
        - Removing the file extension
        - Converting to title case for better readability
        """
        return text.replace("_", " ").split(".")[0].title()

if __name__ == '__main__':
    app = wx.App()
    frame = BibleApp(None)
    frame.Show()
    app.MainLoop()
