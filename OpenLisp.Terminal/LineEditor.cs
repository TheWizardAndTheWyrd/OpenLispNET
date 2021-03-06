﻿/*
 * This implementation of LineEditor is (c) 2015 The Wizard & The Wyrd, LLC
 * Based on the original source for Mono.Terminal.LineEditor
 * Original author: Miguel de Icaza (miguel@novell.com) 
 * Attached is Miguel's original licensing info:
*/

// getline.cs: A command line editor
//
// Authors:
//   Miguel de Icaza (miguel@novell.com)
//
// Copyright 2008 Novell, Inc.
//
// Dual-licensed under the terms of the MIT X11 license or the
// Apache License 2.0
//
// USE -define:DEMO to build this as a standalone file and test it
//
// TODO:
//    Enter an error (a = 1);  Notice how the prompt is in the wrong line
//		This is caused by Stderr not being tracked by System.Console.
//    Completion support
//    Why is Thread.Interrupt not working?   Currently I resort to Abort which is too much.
//
// Limitations in System.Console:
//    Console needs SIGWINCH support of some sort
//    Console needs a way of updating its position after things have been written
//    behind its back (P/Invoke puts for example).
//    System.Console needs to get the DELETE character, and report accordingly.

using System.IO;

namespace OpenLisp.Terminal
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Threading;
    using System.Diagnostics;

    /// <summary>
    /// Provides line editing capabilities in console applications.
    /// </summary>
    public class LineEditor
    {
        /// <summary>
        /// Small object to represent line completion state.
        /// </summary>
        public class Completion
        {
            /// <summary>
            /// The result of completion.
            /// </summary>
            public string[] Result;

            /// <summary>
            /// The prefix for completion.
            /// </summary>
            public string Prefix;

            /// <summary>
            /// Constructor accepting a <see cref="string"/> prefix and
            /// a <see cref="T:string[]"/> result. 
            /// </summary>
            /// <param name="prefix"></param>
            /// <param name="result"></param>
            public Completion(string prefix, string[] result)
            {
                Prefix = prefix;
                Result = result;
            }
        }

        /// <summary>
        /// Delegate to a <see cref="Completion"/> method.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="pos"></param>
        /// <returns></returns>
        public delegate Completion AutoCompleteHandler(string text, int pos);

        //static StreamWriter log;

        // The text being edited.
        StringBuilder _text;

        // The text as it is rendered (replaces (char)1 with ^A on display for example).
        StringBuilder _renderedText;

        // The prompt specified, and the prompt shown to the user.
        string _prompt;
        string _shownPrompt;

        // The current cursor position, indexes into "text", for an index
        // into rendered_text, use TextToRenderPos
        int _cursor;

        // The row where we started displaying data.
        int _homeRow;

        // The maximum length that has been displayed on the screen
        int _maxRendered;

        // If we are done editing, this breaks the interactive loop
        bool _done = false;

        // The thread where the Editing started taking place
#if !NONATIVETHREADS
        Thread _editThread;
#endif
        // Our object that tracks history
        History _history;

        // The contents of the kill buffer (cut/paste in Emacs parlance)
        string _killBuffer = "";

        // The string being searched for
        string _search;
        string _lastSearch;

        // whether we are searching (-1= reverse; 0 = no; 1 = forward)
        int _searching;

        // The position where we found the match.
        int _matchAt;

        // Used to implement the Kill semantics (multiple Alt-Ds accumulate)
        KeyHandler _lastHandler;

        /// <summary>
        /// Delegate to a key handler.
        /// </summary>
        delegate void KeyHandler();

        /// <summary>
        /// Small struct to represent handler state.
        /// </summary>
        struct Handler
        {
            public ConsoleKeyInfo CKI;
            public readonly KeyHandler KeyHandler;

            /// <summary>
            /// Constructor accepting a <see cref="ConsoleKey"/> and a <see cref="LineEditor.KeyHandler"/>.
            /// </summary>
            /// <param name="key"></param>
            /// <param name="h"></param>
            public Handler(ConsoleKey key, KeyHandler h)
            {
                CKI = new ConsoleKeyInfo((char)0, key, false, false, false);
                KeyHandler = h;
            }

            /// <summary>
            /// Constructor accepting a <see cref="char"/> and a <see cref="LineEditor.KeyHandler"/>.
            /// </summary>
            /// <param name="c"></param>
            /// <param name="h"></param>
            private Handler(char c, KeyHandler h)
            {
                KeyHandler = h;
                // Use the "Zoom" as a flag that we only have a character.
                CKI = new ConsoleKeyInfo(c, ConsoleKey.Zoom, false, false, false);
            }

            /// <summary>
            /// Constructor accepting a <see cref="ConsoleKeyInfo"/> and a <see cref="LineEditor.KeyHandler"/>.
            /// </summary>
            /// <param name="cki"></param>
            /// <param name="h"></param>
            private Handler(ConsoleKeyInfo cki, KeyHandler h)
            {
                CKI = cki;
                KeyHandler = h;
            }

            /// <summary>
            /// Control method accepting a <see cref="char"/> and a <see cref="LineEditor.KeyHandler"/>.
            /// </summary>
            /// <param name="c"></param>
            /// <param name="h"></param>
            /// <returns></returns>
            public static Handler Control(char c, KeyHandler h)
            {
                return new Handler((char)(c - 'A' + 1), h);
            }

            /// <summary>
            /// Alt method accepting a <see cref="char"/>, a <see cref="ConsoleKey"/>,
            /// and a <see cref="LineEditor.KeyHandler"/>.
            /// </summary>
            /// <param name="c"></param>
            /// <param name="k"></param>
            /// <param name="h"></param>
            /// <returns></returns>
            public static Handler Alt(char c, ConsoleKey k, KeyHandler h)
            {
                ConsoleKeyInfo cki = new ConsoleKeyInfo((char)c, k, false, true, false);
                return new Handler(cki, h);
            }
        }

        /// <summary>
        ///   Invoked when the user requests auto-completion using the tab character
        /// </summary>
        /// <remarks>
        ///    The result is null for no values found, an array with a single
        ///    string, in that case the string should be the text to be inserted
        ///    for example if the word at pos is "T", the result for a completion
        ///    of "ToString" should be "oString", not "ToString".
        ///
        ///    When there are multiple results, the result should be the full
        ///    text
        /// </remarks>
        public AutoCompleteHandler AutoCompleteEvent;

        /// <summary>
        /// An array of <see cref="Handler"/> objects.
        /// </summary>
        static Handler[] _handlers;

        /// <summary>
        /// Constructor accepting a <see cref="string"/> parameter.
        /// </summary>
        /// <param name="name"></param>
        public LineEditor(string name) : this(name, 10) { }

        /// <summary>
        /// Constructor accepting <see cref="string"/> and <see cref="int"/> parameters.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="histsize"></param>
        public LineEditor(string name, int histsize)
        {
            _handlers = new[] {
                new Handler (ConsoleKey.Home,       CmdHome),
                new Handler (ConsoleKey.End,        CmdEnd),
                new Handler (ConsoleKey.LeftArrow,  CmdLeft),
                new Handler (ConsoleKey.RightArrow, CmdRight),
                new Handler (ConsoleKey.UpArrow,    CmdHistoryPrev),
                new Handler (ConsoleKey.DownArrow,  CmdHistoryNext),
                new Handler (ConsoleKey.Enter,      CmdDone),
                new Handler (ConsoleKey.Backspace,  CmdBackspace),
                new Handler (ConsoleKey.Delete,     CmdDeleteChar),
                new Handler (ConsoleKey.Tab,        CmdTabOrComplete),
				
				// Emacs keys
				Handler.Control ('A', CmdHome),
                Handler.Control ('E', CmdEnd),
                Handler.Control ('B', CmdLeft),
                Handler.Control ('F', CmdRight),
                Handler.Control ('P', CmdHistoryPrev),
                Handler.Control ('N', CmdHistoryNext),
                Handler.Control ('K', CmdKillToEOF),
                Handler.Control ('Y', CmdYank),
                Handler.Control ('D', CmdDeleteChar),
                Handler.Control ('L', CmdRefresh),
                Handler.Control ('R', CmdReverseSearch),
                Handler.Control ('G', delegate {} ),
                Handler.Alt ('B', ConsoleKey.B, CmdBackwardWord),
                Handler.Alt ('F', ConsoleKey.F, CmdForwardWord),

                Handler.Alt ('D', ConsoleKey.D, CmdDeleteWord),
                Handler.Alt ((char) 8, ConsoleKey.Backspace, CmdDeleteBackword),
				
				// DEBUG
				//Handler.Control ('T', CmdDebug),

				// quote
				Handler.Control ('Q', delegate { HandleChar (Console.ReadKey (true).KeyChar); })
            };

            _renderedText = new StringBuilder();
            _text = new StringBuilder();

            _history = new History(name, histsize);

            //if (File.Exists ("log"))File.Delete ("log");
            //log = File.CreateText ("log"); 
        }

        /// <summary>
        /// Dump the history, write a new line, and invoke <see cref="Render"/>.
        /// </summary>
        void CmdDebug()
        {
            _history.Dump();
            Console.WriteLine();
            Render();
        }

        /// <summary>
        /// Render the line editor.
        /// </summary>
        void Render()
        {
            Console.Write(_shownPrompt);
            Console.Write(_renderedText);

            int max = Math.Max(_renderedText.Length + _shownPrompt.Length, _maxRendered);

            for (int i = _renderedText.Length + _shownPrompt.Length; i < _maxRendered; i++)
                Console.Write(' ');
            _maxRendered = _shownPrompt.Length + _renderedText.Length;

            // Write one more to ensure that we always wrap around properly if we are at the
            // end of a line.
            Console.Write(' ');

            UpdateHomeRow(max);
        }

        /// <summary>
        /// Updates home row at the position represented by an <see cref="int"/>.
        /// </summary>
        /// <param name="screenpos"></param>
        void UpdateHomeRow(int screenpos)
        {
            int lines = 0;
            try
            {
                lines = 1 + (screenpos / Console.WindowWidth);
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                lines = 0;
            }


            _homeRow = Console.CursorTop - (lines - 1);
            if (_homeRow < 0)
                _homeRow = 0;
        }


        /// <summary>
        /// Renders from a position represented by an <see cref="int"/>.
        /// </summary>
        /// <param name="pos"></param>
        void RenderFrom(int pos)
        {
            int rpos = TextToRenderPos(pos);
            int i;

            for (i = rpos; i < _renderedText.Length; i++)
                Console.Write(_renderedText[i]);

            if ((_shownPrompt.Length + _renderedText.Length) > _maxRendered)
                _maxRendered = _shownPrompt.Length + _renderedText.Length;
            else
            {
                int max_extra = _maxRendered - _shownPrompt.Length;
                for (; i < max_extra; i++)
                    Console.Write(' ');
            }
        }

        /// <summary>
        /// Compute the rendered line editor text.
        /// </summary>
        void ComputeRendered()
        {
            _renderedText.Length = 0;

            for (int i = 0; i < _text.Length; i++)
            {
                int c = (int)_text[i];
                if (c < 26)
                {
                    if (c == '\t')
                        _renderedText.Append("    ");
                    else
                    {
                        _renderedText.Append('^');
                        _renderedText.Append((char)(c + (int)'A' - 1));
                    }
                }
                else
                    _renderedText.Append((char)c);
            }
        }

        /// <summary>
        /// Renders text at a position represented by an <see cref="int"/>.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        int TextToRenderPos(int pos)
        {
            int p = 0;

            for (int i = 0; i < pos; i++)
            {
                int c;

                c = _text[i];

                if (c >= 26)
                    p++;
                else
                {
                    p += c == 9 ? 4 : 2;
                }
            }

            return p;
        }

        /// <summary>
        /// Renders text to a screen position represented by an <see cref="int"/>.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        int TextToScreenPos(int pos)
        {
            return _shownPrompt.Length + TextToRenderPos(pos);
        }

        /// <summary>
        /// Get and Set the prompt.
        /// </summary>
        string Prompt
        {
            get { return _prompt; }
            set { _prompt = value; }
        }

        /// <summary>
        /// Gets the line editor line count represented by an <see cref="int"/>.
        /// </summary>
        int LineCount => (_shownPrompt.Length + _renderedText.Length) / Console.WindowWidth;

        /// <summary>
        /// Forces the cursos to a new position represented by an <see cref="int"/>.
        /// </summary>
        /// <param name="newpos"></param>
        void ForceCursor(int newpos)
        {
            _cursor = newpos;

            /*
            int actualPos = _shownPrompt.Length + TextToRenderPos(_cursor);
            int row = _homeRow + (actualPos / Console.WindowWidth);
            int col = actualPos % Console.WindowWidth;
            */
            int actualPos = 0;
            int row = 0;
            int col = 0;

            try
            {
                actualPos = _shownPrompt.Length + TextToRenderPos(_cursor);
                row = _homeRow + (actualPos / Console.WindowWidth);
                col = actualPos % Console.WindowWidth;

            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                /*
                actualPos = 0;
                row = 0;
                col = 0;
                */               
            }


            if (row >= Console.BufferHeight)
                row = Console.BufferHeight - 1;
            Console.SetCursorPosition(col, row);

            //log.WriteLine ("Going to cursor={0} row={1} col={2} actual={3} prompt={4} ttr={5} old={6}", newpos, row, col, actual_pos, prompt.Length, TextToRenderPos (cursor), cursor);
            //log.Flush ();
        }

        /// <summary>
        /// Updates cursor at a position represented by an <see cref="int"/>.
        /// </summary>
        /// <param name="newpos"></param>
        void UpdateCursor(int newpos)
        {
            if (_cursor == newpos)
                return;

            ForceCursor(newpos);
        }

        /// <summary>
        /// Inserts a single <see cref="char"/>.
        /// </summary>
        /// <param name="c"></param>
        void InsertChar(char c)
        {
            int prevLines = LineCount;
            _text = _text.Insert(_cursor, c);
            ComputeRendered();
            if (prevLines == LineCount)
            {
                RenderFrom(_cursor);
                ForceCursor(++_cursor);
                UpdateHomeRow(TextToScreenPos(_cursor));
            }
            else
            {
                Console.SetCursorPosition(0, _homeRow);
                Render();
                ForceCursor(++_cursor);
            }
        }
        
        /// <summary>
        /// Done command.
        /// </summary>
        void CmdDone()
        {
            _done = true;
        }

        /// <summary>
        /// Tab or Complete command.
        /// </summary>
        void CmdTabOrComplete()
        {
            bool complete = false;

            if (AutoCompleteEvent == null)
                HandleChar('t');
            else
            {
                if (TabAtStartCompletes)
                    complete = true;
                else
                {
                    for (int i = 0; i < _cursor; i++)
                    {
                        if (Char.IsWhiteSpace(_text[i])) continue;
                        complete = true;
                        break;
                    }
                }

                if (complete)
                {
                    Completion completion = AutoCompleteEvent(_text.ToString(), _cursor);
                    string[] completions = completion.Result;
                    if (completions == null)
                        return;

                    int ncompletions = completions.Length;
                    if (ncompletions == 0)
                        return;

                    if (completions.Length == 1)
                    {
                        InsertTextAtCursor(completions[0]);
                    }
                    else
                    {
                        int last = -1;

                        for (int p = 0; p < completions[0].Length; p++)
                        {
                            char c = completions[0][p];


                            for (int i = 1; i < ncompletions; i++)
                            {
                                if (completions[i].Length < p)
                                    goto mismatch;

                                if (completions[i][p] != c)
                                {
                                    goto mismatch;
                                }
                            }
                            last = p;
                        }
                        mismatch:
                        if (last != -1)
                        {
                            InsertTextAtCursor(completions[0].Substring(0, last + 1));
                        }
                        Console.WriteLine();
                        foreach (string s in completions)
                        {
                            Console.Write(completion.Prefix);
                            Console.Write(s);
                            Console.Write(' ');
                        }
                        Console.WriteLine();
                        Render();
                        ForceCursor(_cursor);
                    }
                }
                else
                    HandleChar('\t');
            }
        }

        /// <summary>
        /// Home command.
        /// </summary>
        void CmdHome()
        {
            UpdateCursor(0);
        }

        /// <summary>
        /// End command.
        /// </summary>
        void CmdEnd()
        {
            UpdateCursor(_text.Length);
        }

        /// <summary>
        /// Left command.
        /// </summary>
        void CmdLeft()
        {
            if (_cursor == 0)
                return;

            UpdateCursor(_cursor - 1);
        }

        /// <summary>
        /// Backward Word command.
        /// </summary>
        void CmdBackwardWord()
        {
            int p = WordBackward(_cursor);
            if (p == -1)
                return;
            UpdateCursor(p);
        }

        /// <summary>
        /// Forward Word command.
        /// </summary>
        void CmdForwardWord()
        {
            int p = WordForward(_cursor);
            if (p == -1)
                return;
            UpdateCursor(p);
        }

        /// <summary>
        /// Right command.
        /// </summary>
        void CmdRight()
        {
            if (_cursor == _text.Length)
                return;

            UpdateCursor(_cursor + 1);
        }

        /// <summary>
        /// Render after a position represented by an <see cref="int"/>.
        /// </summary>
        /// <param name="p"></param>
        void RenderAfter(int p)
        {
            ForceCursor(p);
            RenderFrom(p);
            ForceCursor(_cursor);
        }

        /// <summary>
        /// Backspace command.
        /// </summary>
        void CmdBackspace()
        {
            if (_cursor == 0)
                return;

            _text.Remove(--_cursor, 1);
            ComputeRendered();
            RenderAfter(_cursor);
        }

        /// <summary>
        /// Delete Char command.
        /// </summary>
        void CmdDeleteChar()
        {
            // If there is no input, this behaves like EOF
            if (_text.Length == 0)
            {
                _done = true;
                _text = null;
                Console.WriteLine();
                return;
            }

            if (_cursor == _text.Length)
                return;
            _text.Remove(_cursor, 1);
            ComputeRendered();
            RenderAfter(_cursor);
        }

        /// <summary>
        /// Word Forward to a position represented by an <see cref="int"/>.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        int WordForward(int p)
        {
            if (p >= _text.Length)
                return -1;

            int i = p;
            if (Char.IsPunctuation(_text[p]) || Char.IsSymbol(_text[p]) || Char.IsWhiteSpace(_text[p]))
            {
                for (; i < _text.Length; i++)
                {
                    if (Char.IsLetterOrDigit(_text[i]))
                        break;
                }
                for (; i < _text.Length; i++)
                {
                    if (!Char.IsLetterOrDigit(_text[i]))
                        break;
                }
            }
            else
            {
                for (; i < _text.Length; i++)
                {
                    if (!Char.IsLetterOrDigit(_text[i]))
                        break;
                }
            }
            if (i != p)
                return i;
            return -1;
        }

        /// <summary>
        /// Word Backward to a point represented by an <see cref="int"/>.
        /// </summary>
        /// <param name="p"></param>
        /// <returns></returns>
        int WordBackward(int p)
        {
            if (p == 0)
                return -1;

            int i = p - 1;
            if (i == 0)
                return 0;

            if (Char.IsPunctuation(_text[i]) || Char.IsSymbol(_text[i]) || Char.IsWhiteSpace(_text[i]))
            {
                for (; i >= 0; i--)
                {
                    if (Char.IsLetterOrDigit(_text[i]))
                        break;
                }
                for (; i >= 0; i--)
                {
                    if (!Char.IsLetterOrDigit(_text[i]))
                        break;
                }
            }
            else
            {
                for (; i >= 0; i--)
                {
                    if (!Char.IsLetterOrDigit(_text[i]))
                        break;
                }
            }
            i++;

            if (i != p)
                return i;

            return -1;
        }

        /// <summary>
        /// Delete Word command.
        /// </summary>
        void CmdDeleteWord()
        {
            int pos = WordForward(_cursor);

            if (pos == -1)
                return;

            string k = _text.ToString(_cursor, pos - _cursor);

            if (_lastHandler == CmdDeleteWord)
                _killBuffer = _killBuffer + k;
            else
                _killBuffer = k;

            _text.Remove(_cursor, pos - _cursor);
            ComputeRendered();
            RenderAfter(_cursor);
        }

        /// <summary>
        /// Delete Backword command.
        /// </summary>
        void CmdDeleteBackword()
        {
            int pos = WordBackward(_cursor);
            if (pos == -1)
                return;

            string k = _text.ToString(pos, _cursor - pos);

            if (_lastHandler == CmdDeleteBackword)
                _killBuffer = k + _killBuffer;
            else
                _killBuffer = k;

            _text.Remove(pos, _cursor - pos);
            ComputeRendered();
            RenderAfter(pos);
        }

        /// <summary>
        /// Adds the current line to the history if needed
        /// </summary>
        void HistoryUpdateLine()
        {
            _history.Update(_text.ToString());
        }

        /// <summary>
        /// Previous History command.
        /// </summary>
        void CmdHistoryPrev()
        {
            if (!_history.PreviousAvailable())
                return;

            HistoryUpdateLine();

            SetText(_history.Previous());
        }

        /// <summary>
        /// History Next command.
        /// </summary>
        void CmdHistoryNext()
        {
            if (!_history.NextAvailable())
                return;

            _history.Update(_text.ToString());
            SetText(_history.Next());

        }

        /// <summary>
        /// Kill to EOF command.
        /// </summary>
        void CmdKillToEOF()
        {
            _killBuffer = _text.ToString(_cursor, _text.Length - _cursor);
            _text.Length = _cursor;
            ComputeRendered();
            RenderAfter(_cursor);
        }

        /// <summary>
        /// Yank command.
        /// </summary>
        void CmdYank()
        {
            InsertTextAtCursor(_killBuffer);
        }

        /// <summary>
        /// Insert text represented by a <see cref="string"/> at the cursor.
        /// </summary>
        /// <param name="str"></param>
        void InsertTextAtCursor(string str)
        {
            int prev_lines = LineCount;
            _text.Insert(_cursor, str);
            ComputeRendered();
            if (prev_lines != LineCount)
            {
                Console.SetCursorPosition(0, _homeRow);
                Render();
                _cursor += str.Length;
                ForceCursor(_cursor);
            }
            else
            {
                RenderFrom(_cursor);
                _cursor += str.Length;
                ForceCursor(_cursor);
                UpdateHomeRow(TextToScreenPos(_cursor));
            }
        }

        /// <summary>
        /// Set the Search Prompt.
        /// </summary>
        /// <param name="s"></param>
        void SetSearchPrompt(string s)
        {
            SetPrompt("(reverse-i-search)`" + s + "': ");
        }

        /// <summary>
        /// Reverse Search through <see cref="_text"/>.
        /// </summary>
        void ReverseSearch()
        {
            int p;

            if (_cursor == _text.Length)
            {
                // The cursor is at the end of the string

                p = _text.ToString().LastIndexOf(_search);
                if (p != -1)
                {
                    _matchAt = p;
                    _cursor = p;
                    ForceCursor(_cursor);
                    return;
                }
            }
            else
            {
                // The cursor is somewhere in the middle of the string
                int start = (_cursor == _matchAt) ? _cursor - 1 : _cursor;
                if (start != -1)
                {
                    p = _text.ToString().LastIndexOf(_search, start);
                    if (p != -1)
                    {
                        _matchAt = p;
                        _cursor = p;
                        ForceCursor(_cursor);
                        return;
                    }
                }
            }

            // Need to search backwards in history
            HistoryUpdateLine();
            string s = _history.SearchBackward(_search);
            if (s != null)
            {
                _matchAt = -1;
                SetText(s);
                ReverseSearch();
            }
        }

        /// <summary>
        /// Reverse Search command.
        /// </summary>
        void CmdReverseSearch()
        {
            if (_searching == 0)
            {
                _matchAt = -1;
                _lastSearch = _search;
                _searching = -1;
                _search = "";
                SetSearchPrompt("");
            }
            else
            {
                if (_search == "")
                {
                    if (_lastSearch != "" && _lastSearch != null)
                    {
                        _search = _lastSearch;
                        SetSearchPrompt(_search);

                        ReverseSearch();
                    }
                    return;
                }
                ReverseSearch();
            }
        }

        /// <summary>
        /// Search Append with a <see cref="char"/>.
        /// </summary>
        /// <param name="c"></param>
        void SearchAppend(char c)
        {
            _search = _search + c;
            SetSearchPrompt(_search);

            //
            // If the new typed data still matches the current text, stay here
            //
            if (_cursor < _text.Length)
            {
                string r = _text.ToString(_cursor, _text.Length - _cursor);
                if (r.StartsWith(_search))
                    return;
            }

            ReverseSearch();
        }

        /// <summary>
        /// Refresh command.
        /// </summary>
        void CmdRefresh()
        {
            Console.Clear();
            _maxRendered = 0;
            Render();
            ForceCursor(_cursor);
        }

        /// <summary>
        /// Event to interrupt editing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="a"></param>
        void InterruptEdit(object sender, ConsoleCancelEventArgs a)
        {
            // Do not abort our program:
            a.Cancel = true;

            // Interrupt the editor
#if !NONATIVETHREADS
            _editThread.Abort();
#endif
        }

        void HandleChar(char c)
        {
            if (_searching != 0)
                SearchAppend(c);
            else
                InsertChar(c);
        }

        /// <summary>
        /// Edit the loop.
        /// </summary>
        void EditLoop()
        {
            ConsoleKeyInfo cki;

            while (!_done)
            {
                ConsoleModifiers mod;

                cki = Console.ReadKey(true);
                if (cki.Key == ConsoleKey.Escape)
                {
                    cki = Console.ReadKey(true);

                    mod = ConsoleModifiers.Alt;
                }
                else
                    mod = cki.Modifiers;

                bool handled = false;

                foreach (Handler handler in _handlers)
                {
                    ConsoleKeyInfo t = handler.CKI;

                    if (t.Key == cki.Key && t.Modifiers == mod)
                    {
                        handled = true;
                        handler.KeyHandler();
                        _lastHandler = handler.KeyHandler;
                        break;
                    }
                    else if (t.KeyChar == cki.KeyChar && t.Key == ConsoleKey.Zoom)
                    {
                        handled = true;
                        handler.KeyHandler();
                        _lastHandler = handler.KeyHandler;
                        break;
                    }
                }
                if (handled)
                {
                    if (_searching != 0)
                    {
                        if (_lastHandler != CmdReverseSearch)
                        {
                            _searching = 0;
                            SetPrompt(_prompt);
                        }
                    }
                    continue;
                }

                if (cki.KeyChar != (char)0)
                    HandleChar(cki.KeyChar);
            }
        }

        /// <summary>
        /// Initial text.
        /// </summary>
        /// <param name="initial"></param>
        void InitText(string initial)
        {
            _text = new StringBuilder(initial);
            ComputeRendered();
            _cursor = _text.Length;
            Render();
            ForceCursor(_cursor);
        }

        /// <summary>
        /// Set text at a cursor with a <see cref="string"/> value.
        /// </summary>
        /// <param name="newtext"></param>
        void SetText(string newtext)
        {
            Console.SetCursorPosition(0, _homeRow);
            InitText(newtext);
        }

        /// <summary>
        /// Set the prompt with a <see cref="string"/> value.
        /// </summary>
        /// <param name="newprompt"></param>
        void SetPrompt(string newprompt)
        {
            _shownPrompt = newprompt;
            Console.SetCursorPosition(0, _homeRow);
            Render();
            ForceCursor(_cursor);
        }

        /// <summary>
        /// Edits the line editor.
        /// </summary>
        /// <param name="prompt"></param>
        /// <param name="initial"></param>
        /// <returns></returns>
        public string Edit(string prompt, string initial)
        {
#if !NONATIVETHREADS
            _editThread = Thread.CurrentThread;
#endif
            _searching = 0;
            Console.CancelKeyPress += InterruptEdit;

            _done = false;
            _history.CursorToEnd();
            _maxRendered = 0;

            Prompt = prompt;
            _shownPrompt = prompt;
            InitText(initial);
            _history.Append(initial);

            do
            {
                try
                {
                    EditLoop();
                }
                catch (ThreadAbortException)
                {
                    _searching = 0;
#if !NONATIVETHREADS
                    Thread.ResetAbort();
#endif
                    Console.WriteLine();
                    SetPrompt(prompt);
                    SetText("");
                }
            } while (!_done);
            Console.WriteLine();

            Console.CancelKeyPress -= InterruptEdit;

            if (_text == null)
            {
                _history.Close();
                return null;
            }

            string result = _text.ToString();
            if (result != "")
                _history.Accept(result);
            else
                _history.RemoveLast();

            return result;
        }

        /// <summary>
        /// Saves the command history.
        /// </summary>
        public void SaveHistory()
        {
            if (_history != null)
            {
                _history.Close();
            }
        }

        /// <summary>
        /// Get or Set the tab at start completes.
        /// </summary>
        public bool TabAtStartCompletes { get; set; }

        /// <summary>
        /// Emulates the bash-like behavior, where edits done to the
        /// history are recorded.
        /// </summary>
        class History
        {
            string[] history;
            int head, tail;
            int cursor, count;
            string histfile;

            public History(string app, int size)
            {
                if (size < 1)
                    throw new ArgumentException("size");

                if (app != null)
                {
                    string dir = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
                    //Console.WriteLine (dir);
                    /*
					if (!Directory.Exists (dir)){
						try {
							Directory.CreateDirectory (dir);
						} catch {
							app = null;
						}
					}
					if (app != null)
						histfile = Path.Combine (dir, app) + ".history";
					*/
                    histfile = Path.Combine(dir, ".mal-history");
                }

                history = new string[size];
                head = tail = cursor = 0;

                if (File.Exists(histfile))
                {
                    using (StreamReader sr = File.OpenText(histfile))
                    {
                        string line;

                        while ((line = sr.ReadLine()) != null)
                        {
                            if (line != "")
                                Append(line);
                        }
                    }
                }
            }

            /// <summary>
            /// Closes the history file.
            /// </summary>
            public void Close()
            {
                if (histfile == null)
                    return;

                try
                {
                    using (StreamWriter sw = File.CreateText(histfile))
                    {
                        int start = (count == history.Length) ? head : tail;
                        for (int i = start; i < start + count; i++)
                        {
                            int p = i % history.Length;
                            sw.WriteLine(history[p]);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[{DateTime.Now}] Error: {ex.Message}\n{ex.StackTrace}");
                }
            }

            /// <summary>
            /// Appends a value to the history
            /// </summary>
            /// <param name="s"></param>
            public void Append(string s)
            {
                //Console.WriteLine ("APPENDING {0} head={1} tail={2}", s, head, tail);
                history[head] = s;
                head = (head + 1) % history.Length;
                if (head == tail)
                    tail = (tail + 1 % history.Length);
                if (count != history.Length)
                    count++;
                //Console.WriteLine ("DONE: head={1} tail={2}", s, head, tail);
            }

            /// <summary>
            /// Updates the current cursor location with the string,
            /// to support editing of history items.   For the current
            /// line to participate, an Append must be done before.
            /// </summary>
            /// <param name="s"></param>
            public void Update(string s)
            {
                history[cursor] = s;
            }

            /// <summary>
            /// Removes last command from the history.
            /// </summary>
            public void RemoveLast()
            {
                head = head - 1;
                if (head < 0)
                    head = history.Length - 1;
            }

            /// <summary>
            /// Accepts a <see cref="string"/>.
            /// </summary>
            /// <param name="s"></param>
            public void Accept(string s)
            {
                int t = head - 1;
                if (t < 0)
                    t = history.Length - 1;

                history[t] = s;
            }

            /// <summary>
            /// Is the previous command available?
            /// </summary>
            /// <returns></returns>
            public bool PreviousAvailable()
            {
                //Console.WriteLine ("h={0} t={1} cursor={2}", head, tail, cursor);
                if (count == 0)
                    return false;
                int next = cursor - 1;
                if (next < 0)
                    next = count - 1;

                if (next == head)
                    return false;

                return true;
            }

            /// <summary>
            /// Is the next command available?
            /// </summary>
            /// <returns></returns>
            public bool NextAvailable()
            {
                if (count == 0)
                    return false;
                int next = (cursor + 1) % history.Length;
                if (next == head)
                    return false;
                return true;
            }

            /// <summary>
            /// Returns a string with the previous line contents, or
            /// null if there is no data in the history to move to.
            /// </summary>
            /// <returns></returns>
            public string Previous()
            {
                if (!PreviousAvailable())
                    return null;

                cursor--;
                if (cursor < 0)
                    cursor = history.Length - 1;

                return history[cursor];
            }

            /// <summary>
            /// Gets the next command in the history.
            /// </summary>
            /// <returns></returns>
            public string Next()
            {
                if (!NextAvailable())
                    return null;

                cursor = (cursor + 1) % history.Length;
                return history[cursor];
            }

            /// <summary>
            /// Sets the cursor to the end.
            /// </summary>
            public void CursorToEnd()
            {
                if (head == tail)
                    return;

                cursor = head;
            }

            /// <summary>
            /// Dumps the command history.
            /// </summary>
            public void Dump()
            {
                Console.WriteLine("Head={0} Tail={1} Cursor={2} count={3}", head, tail, cursor, count);
                for (int i = 0; i < history.Length; i++)
                {
                    Console.WriteLine(" {0} {1}: {2}", i == cursor ? "==>" : "   ", i, history[i]);
                }
                //log.Flush ();
            }

            /// <summary>
            /// Search backards for a term represented by a <see cref="string"/>.
            /// </summary>
            /// <param name="term"></param>
            /// <returns></returns>
            public string SearchBackward(string term)
            {
                for (int i = 0; i < count; i++)
                {
                    int slot = cursor - i - 1;
                    if (slot < 0)
                        slot = history.Length + slot;
                    if (slot >= history.Length)
                        slot = 0;
                    if (history[slot] != null && history[slot].IndexOf(term) != -1)
                    {
                        cursor = slot;
                        return history[slot];
                    }
                }

                return null;
            }

        }
    }

#if DEMO
	class Demo {
		static void Main ()
		{
			LineEditor le = new LineEditor ("foo");
			string s;
			
			while ((s = le.Edit ("shell> ", "")) != null){
				Console.WriteLine ("----> [{0}]", s);
			}
		}
	}
#endif
}
