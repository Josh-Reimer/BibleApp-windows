import sys
from PyQt6.QtWidgets import QApplication, QWidget, QVBoxLayout, QHBoxLayout, QLabel, QLineEdit, QPushButton, QTextEdit, QComboBox
from PyQt6.QtCore import Qt
import pyperclip
import ChapterVerse as CV
import list_and_str_ops as list
from BibleFileNames import files
from charEliminator import onlyVerses, remove_only_chars

# Define the list of Bible text files
bible_text_files = ["genesis.txt", "exodus.txt", "leviticus.txt", "numbers.txt", "deuteronomy.txt", "joshua.txt", 
                    "judges.txt", "ruth.txt", "first_samuel.txt", "second_samuel.txt", "first_kings.txt", "second_kings.txt", 
                    "first_chronicles.txt", "second_chronicles.txt", "ezra.txt", "nehemiah.txt", "esther.txt", "job.txt", 
                    "psalms.txt", "proverbs.txt", "eccliasiastes.txt", "song_of_solomon.txt", "isaiah.txt", "jeremiah.txt", 
                    "lamentations.txt", "ezekial.txt", "daniel.txt", "hosea.txt", "joel.txt", "amos.txt", "obadiah.txt", 
                    "jonah.txt", "micah.txt", "nahum.txt", "habakkuk.txt", "zephaniah.txt", "haggai.txt", "zechariah.txt", 
                    "malachi.txt", "matthew.txt", "mark.txt", "luke.txt", "john.txt", "acts.txt", "romans.txt", 
                    "first_corinthians.txt", "second_corinthians.txt", "galatians.txt", "ephesians.txt", "philipians.txt", 
                    "colossians.txt", "first_thesselonians.txt", "second_thesselonians.txt", "first_timothy.txt", 
                    "second_timothy.txt", "titus.txt", "philemon.txt", "hebrews.txt", "james.txt", "first_peter.txt", 
                    "second_peter.txt", "first_john.txt", "second_john.txt", "third_john.txt", "jude.txt", "revelation.txt"]

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
        
        # Create the "Home" button
        home_btn = QPushButton('HOME', self)
        home_btn.clicked.connect(self.on_home)
        
        search_btn = QPushButton('SEARCH', self)
        search_btn.clicked.connect(self.on_search)
        
        clear_btn = QPushButton('Clear', self)
        clear_btn.clicked.connect(self.on_clear)

        # Create the ComboBox for book selection
        self.book_combo_box = QComboBox(self)
        self.populate_combo_box()  # Populate the ComboBox with formatted book names
        self.book_combo_box.currentIndexChanged.connect(self.on_book_select)  # Connect to selection handler
        
        # Add widgets to the horizontal layout
        hbox.addWidget(lbl)
        hbox.addWidget(self.search_input)
        hbox.addWidget(search_btn)
        hbox.addWidget(home_btn)  # Home button goes first
        hbox.addWidget(clear_btn)
        hbox.addWidget(self.book_combo_box)  # Add ComboBox for books

        # Add the horizontal layout to the vertical layout
        vbox.addLayout(hbox)

        # Create a text area for displaying the results
        self.result_area = QTextEdit(self)
        self.result_area.setReadOnly(True)
        vbox.addWidget(self.result_area)

        # Set the main layout for the window
        self.setLayout(vbox)

    def populate_combo_box(self):
        """Populate the ComboBox with book names (no file extensions, capitalized, no underscores)."""
        for file in bible_text_files:
            book_name = self.clean(file)
            self.book_combo_box.addItem(book_name)

    def on_search(self):
        """Handle search button click."""
        searchkey = self.search_input.text()
        results = self.linear_search(searchkey)
        self.result_area.setText(results)

    def on_clear(self):
        """Handle clear button click."""
        self.search_input.clear()
        self.result_area.clear()

    def on_home(self):
        """Handle home button click, show Genesis content and clear search input."""
        self.search_input.clear()  # Clear the search input
        try:
            with open("genesis.txt", "r") as f:
                genesis_content = f.read()
            self.result_area.setText(genesis_content)  # Display content of genesis.txt
        except FileNotFoundError:
            self.result_area.setText("Genesis file not found.")

    def on_book_select(self):
        """Handle book selection from the ComboBox."""
        selected_book = self.book_combo_box.currentText().lower() + ".txt"
        try:
            with open(selected_book, "r") as f:
                content = f.read()
            self.result_area.setText(content)  # Display content of the selected book
        except FileNotFoundError:
            self.result_area.setText(f"{selected_book} file not found.")

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
