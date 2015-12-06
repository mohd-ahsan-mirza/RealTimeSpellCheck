using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using SpellCheck;
using System.Text.RegularExpressions;

namespace RealTimeSpellingCheck
{
    public partial class Form1 : Form
    {
        // Dictionary class for spell correction
        Dictionary d;

        // index of the first char for a word in the textbox
        int startPosition;
        // Equals to the length of the text in the word
        int currentPosition;
        // checks whether enter has been pressed for autocompletion
        bool isEnterPressed;
        // Possible replacement word used for autocompletion 
        string suggestion;
        // second last word in the textbox
        string previousWord;
        // List of all the indexs for startingPositions (Used in case of BackSpace)
        List<int> startingPositions;
        //In case comboBox is present in the textBox
        bool comboPresent;

        public Form1()
        {
            InitializeComponent();

            // Initilazing the variables

            this.d = new Dictionary("file.txt");

            startPosition = 0;
            currentPosition = 0;
            isEnterPressed = false;
            suggestion = "";
            this.previousWord = "";
            startingPositions = new List<int>();
            this.comboPresent = false;
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {
            // Casting object passed to RichTextBox 
            RichTextBox text = (RichTextBox)sender;

            //Clearing all the controls from the richTextBox in order to restart all over again
            text.Controls.Clear();

            currentPosition = text.Text.Length;

            //In case enter key was pressed for autocompletion
            if (!isEnterPressed)
            {
                //Handles events when a key pressed for RichTextBox 
                text.KeyDown += new KeyEventHandler(richTextBox1_KeyDown);

                // Segment of letters from startPosition to the end of text 
                string segment = text.Text.Substring(startPosition, currentPosition - startPosition);

                // If segment contains only letters
                if (Regex.IsMatch(segment, @"^[a-zA-Z]+$"))
                {
                    //When the segment is more than one letter
                    if (currentPosition - startPosition != 1)
                    {
                        //If the segment doesn't exists in the data structure
                        if (!d.LookUp(segment.ToLower()))
                        {
                            //Select the segment and underline it

                            text.SelectionStart = startPosition;
                            text.SelectionLength = currentPosition;
                            text.SelectionFont = new Font("Times New Roman", 10, FontStyle.Underline);
                            text.SelectedText = segment;

                            //Create a new comboBox for containing possible matching words for replacement
                            ComboBox combo = new ComboBox();
                            combo.Items.AddRange(d.suggestions(segment.ToLower()).ToArray());

                            //If the combo is not empty
                            if (combo.Items.Count != 0)
                            {
                                // The position of the combobox is right after where the segment is in the RichTextBox
                                Point point = text.GetPositionFromCharIndex(currentPosition);
                                combo.Location = new Point(point.X + 2, point.Y);
                                // Style is simple so whole list is visible when displayed
                                combo.DropDownStyle = ComboBoxStyle.Simple;
                                //Enables the user to just type in the comboBox to select an item from it.
                                /*  
                                combo.AutoCompleteMode = AutoCompleteMode.Suggest;
                                combo.AutoCompleteSource = AutoCompleteSource.ListItems;
                                */
                                //Background color and name 
                                combo.BackColor = SystemColors.ControlLight;
                                combo.Name = "replacementCombo";
                                // Event handler in case a key is pressed when comboBox has focus
                                combo.KeyDown += new KeyEventHandler(replacementCombo_KeyDown);
                                text.Controls.Add(combo);
                                //Combo is present
                                this.comboPresent = true;
                            }
                        }
                        else
                        {
                            //If segment exists, select the segment and restore original font(Required in case user presses backspace)

                            text.SelectionStart = startPosition;
                            text.SelectionLength = currentPosition;
                            text.SelectionFont = new Font("Times New Roman", 10, FontStyle.Regular);
                            text.SelectedText = segment;

                            /*
                                One drawback of using this if statement is that once the user types in a correct word
                                even though it might not be a word that you desire, no word will be suggested
                                for autocompletion
                            */
                            //If segment is not a word
                            if (!d.isAWord(segment.ToLower()))
                            {
                                //Gets the most frequently used word for the segment
                                this.suggestion = d.getMostPopularWordForSegment(segment.ToLower());

                                //If the first char of the segment string is upper case
                                if (Char.IsUpper(segment[0]))
                                    suggestion=suggestion.Substring(0, 1).ToUpper() + suggestion.Substring(1);

                                //Same concept as comboBox above

                                Label label = new Label();
                                label.Location = text.GetPositionFromCharIndex(currentPosition);
                                label.Location = new Point(label.Location.X + 2, label.Location.Y);
                                label.AutoSize = true;
                                label.Text = suggestion;
                                label.BackColor = SystemColors.ControlLight;
                                text.Controls.Add(label);

                            }
                            else
                            {
                                this.previousWord = segment.ToLower();
                            }
                        }

                    }

                }
                //If segment contains char(s) other than letters.It means that it's time to move on
                else
                {
                    //Change the starting Position
                    this.startingPositions.Add(this.startPosition);
                    this.startPosition = this.currentPosition;

                    // Increasing the popularity of the word that was used
                    if (this.previousWord.Length != 0)
                    {
                        d.changeWordPopularity(this.previousWord.ToLower());
                        this.previousWord = "";
                    }
                }
            }
            //If isEnterPressed is true
            else
            {
                this.isEnterPressed = false;
            }
            
        }

        private void richTextBox1_KeyDown(object sender,KeyEventArgs e)
        {
            //If enter was presed while richTextBox has focus
            if (e.KeyCode == Keys.Enter)
            {
                //If a word was selected for autocompletion by user
                if (this.suggestion.Length!=0)
                {
                    this.isEnterPressed = true;
                    //Erase the segment from the textBox
                    richTextBox1.Text = richTextBox1.Text.Substring(0, startPosition);
                    // Append the suggested with the text that was previous to the segment that was erased above
                    richTextBox1.Text = richTextBox1.Text+suggestion;
                    // Suggested word becomes previous
                    this.previousWord = suggestion.ToLower();
                    // Reseting 
                    suggestion = "";

                    //Stops from enter key from completing it's default task,i.e. starting a new line
                    e.Handled = true;
                }
            }

            //When backspace key is pressed
            if (e.KeyCode == Keys.Back)
            {
                // currentPosition can never less than startPosition and there has to be text 
                if(richTextBox1.TextLength!=0 && this.currentPosition==this.startPosition)
                {
                    //Moves the startPosition to it's previous index
                    this.startPosition = this.startingPositions.ElementAt(this.startingPositions.Count - 1);
                    //Removes that index from the list
                    this.startingPositions.RemoveAt(this.startingPositions.Count - 1);
                }
            }

            //When right arrow key is pressed
            if (e.KeyCode == Keys.Right)
            {
                //If a comboBox is present
                if (this.comboPresent)
                {
                    //variable becomes false and Tab key press is simulated (for entering in the comboBox
                    this.comboPresent = false;
                    SendKeys.Send("{TAB}");
                }
            }

        }

        private void replacementCombo_KeyDown(object sender, KeyEventArgs e)
        {
            ComboBox combo = (ComboBox)sender;

            if (e.KeyCode == Keys.Enter)
            {
                //Same concept as above when enter key is pressed in case of RichextBox

                this.isEnterPressed = true;
                bool upper = Char.IsUpper(richTextBox1.Text[startPosition]);
                richTextBox1.Text = richTextBox1.Text.Substring(0, startPosition);
                if (upper)
                    richTextBox1.Text = richTextBox1.Text + combo.Text.Substring(0, 1).ToUpper() + combo.Text.Substring(1);
                else
                    richTextBox1.Text = richTextBox1.Text + combo.Text;
                this.previousWord = combo.Text.ToLower();

                e.Handled = true;   
            }

            //When left arrow key is pressed
            if(e.KeyCode == Keys.Left)
            {
                //If nothing is selected
                if(combo.Text.Length==0)
                {
                    //For leaving the comboBox
                    SendKeys.Send("{TAB}");
                    //In case the user wants to re-enter
                    this.comboPresent = true;
                }
            }

        }


    }
}
