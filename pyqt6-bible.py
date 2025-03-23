import sys
from PyQt6.QtWidgets import QApplication, QWidget, QVBoxLayout, QHBoxLayout, QLabel, QLineEdit, QPushButton, QTextEdit
from PyQt6.QtCore import Qt
import pyperclip
import ChapterVerse as CV
import list_and_str_ops as list
from BibleFileNames import files
from charEliminator import onlyVerses, remove_only_chars


class BibleApp(QWidget):
    def __init__(self):
        super().__init__()
        self.init_ui()

    def init_ui(self):
        """Initialize the user interface."""
        self.setWindowTitle("Desktop Bible")
        self.setGeometry(100, 100, 800, 600)
        
        # Create the main layout (vertical layout)
        vbox = QVBoxLayout()

        # Create the horizontal layout for input and buttons
        hbox = QHBoxLayout()

        # Create the label, text input field, and buttons
        lbl = QLabel('Enter search keywords: ')
        self.search_input = QLineEdit(self)
        search_btn = QPushButton('SEARCH', self)
        search_btn.clicked.connect(self.on_search)
        clear_btn = QPushButton('Clear', self)
        clear_btn.clicked.connect(self.on_clear)

        # Add widgets to the horizontal layout
        hbox.addWidget(lbl)
        hbox.addWidget(self.search_input)
        hbox.addWidget(search_btn)
        hbox.addWidget(clear_btn)

        # Add the horizontal layout to the vertical layout
        vbox.addLayout(hbox)

        # Create a text area for displaying the results
        self.result_area = QTextEdit(self)
        self.result_area.setReadOnly(True)
        vbox.addWidget(self.result_area)

        # Set the main layout for the window
        self.setLayout(vbox)

    def on_search(self):
        """Handle search button click."""
        searchkey = self.search_input.text()
        results = self.linear_search(searchkey)
        self.result_area.setText(results)

    def on_clear(self):
        """Handle clear button click."""
        self.search_input.clear()
        self.result_area.clear()

    def linear_search(self, searchkey):
        """Perform a linear search for verses containing the search keyword."""
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
        """Clean the book name by removing underscores and file extension."""
        return text.replace("_", " ").split(".")[0].title()


if __name__ == '__main__':
    app = QApplication(sys.argv)
    window = BibleApp()
    window.show()
    sys.exit(app.exec())
