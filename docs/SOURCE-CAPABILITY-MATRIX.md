# Source Capability Matrix

This matrix records automatically detected capability signals. It is an input to manual import review, not a substitute for code inspection.

| Project | Inventory management | Image ingestion | Image root management | OCR | OpenCV preprocessing | Card detection | Sports-card identification | TCG identification | Front/back image pairing | Duplicate detection | Metadata normalization | File renaming | File organization | Pricing | Comparable sales | Lot recommendations | eBay listing generation | Existing eBay listing import | Listing audits | CSV import/export | Background jobs | Desktop UI | Linux compatibility | iOS compatibility | Android compatibility | Tests | Installer/release automation | Logging and diagnostics | Disposition |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| html2pdf |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  | REJECT |
| html_md_converter |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| csv_to_json |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | REJECT |
| Download-page-as-pdf |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| pdf_to_text |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| ballBurstingGameOpenCV |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| pdf2text |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| pdf_redaction |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| currency_converter |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| send_telegram_message |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| tearDrops-Discord_Bot |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  | REFERENCE_ONLY |
| instaGram_Bot | yes | yes |  |  |  |  |  |  | yes | yes |  |  |  |  |  |  |  |  |  | yes |  |  | yes |  |  |  |  |  | REFERENCE_ONLY |
| send-discord_message |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| aws_s3_data_download |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| ec2_launcher |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| unfollowers-insta |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  | REFERENCE_ONLY |
| simple-image-encryptor |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| gradient_generator |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| image-scrapper |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| exif_viewer |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| facial-keypoints-detection |  |  |  | yes | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| image_resizer |  |  |  |  |  |  |  |  |  |  |  | yes | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| image_watermark |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| image_caption_generator |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| image_converter |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| Convert images to JPEG |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| rock-paper-scissors-game |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| Space Shooter ( Pygame ) |  |  |  |  |  |  |  |  | yes |  | yes |  | yes |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  | REFERENCE_ONLY |
| gui_games |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | REJECT |
| pongGame |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| breast_cancer_detection |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| certificate_generator |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| speed_game |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| tambola_game_generator |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| ig_bot_auto_upload_img | yes | yes |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  | yes |  |  | yes |  |  |  |  |  | REFERENCE_ONLY |
| SwiftFormat |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  | yes |  |  |  | yes |  |  | yes | yes |  | yes | yes |  | REFERENCE_ONLY |
| SwiftLint |  |  |  |  |  |  |  |  | yes | yes |  |  |  |  |  | yes |  |  | yes | yes |  |  | yes | yes |  | yes | yes |  | IMPORT_AND_REFACTOR |
| studious-guide |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| superagent_playarts |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| transparent-background |  | yes |  | yes | yes |  |  |  | yes |  |  |  | yes | yes |  |  |  |  |  | yes | yes |  | yes |  |  | yes | yes |  | IMPORT_AND_REFACTOR |
| USBStealer |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| tailor |  |  |  |  |  |  |  |  | yes | yes |  |  |  |  |  |  | yes |  |  | yes |  |  | yes | yes | yes |  | yes |  | IMPORT_AND_REFACTOR |
| TorrentMorph |  |  |  |  |  |  | yes |  | yes | yes | yes | yes | yes |  |  |  | yes |  |  | yes |  |  | yes |  |  | yes |  | yes | REFERENCE_ONLY |
| StosVPN |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| software_backup | yes |  |  |  |  |  |  |  | yes | yes |  |  | yes |  |  |  |  |  |  | yes |  |  | yes |  |  |  |  | yes | REFERENCE_ONLY |
| spleeter |  |  |  |  |  |  |  |  | yes |  |  |  | yes | yes |  |  | yes |  |  |  |  |  | yes |  |  | yes | yes |  | REFERENCE_ONLY |
| Social-Media-Projects |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  |  |  | yes |  | yes | yes | yes |  |  |  |  | yes | yes |  | IMPORT_AND_REFACTOR |
| SoftwareSelector |  |  | yes |  |  |  |  |  | yes |  | yes |  | yes |  |  |  | yes |  |  | yes |  | yes | yes |  |  | yes | yes | yes | IMPORT_AND_REFACTOR |
| Stamplicity |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| stamplicity-V2.0 | yes |  |  |  |  |  | yes |  |  |  | yes |  |  | yes |  |  | yes |  | yes |  |  | yes |  | yes | yes | yes |  |  | REFERENCE_ONLY |
| spotify-downloader |  |  |  |  |  |  | yes |  | yes |  | yes | yes |  |  |  |  | yes |  |  |  |  | yes | yes |  |  | yes | yes |  | IMPORT_AND_REFACTOR |
| stamp-valuer-ai |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| File_Transfer_Protocol |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| pawchain-carechain-complete |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| worker-publisher-template |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  |  |  |  |  | yes |  | REFERENCE_ONLY |
| _AI_ORG |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| download_mp3 |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| voice input output |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| pawchain-carechain-v1 |  |  |  |  |  |  |  |  | yes |  | yes |  |  |  |  | yes | yes |  | yes |  |  |  |  |  |  | yes |  |  | REFERENCE_ONLY |
| audiobook |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  | yes |  |  |  |  |  | REJECT |
| windows11 |  |  |  |  |  |  | yes |  | yes |  |  |  | yes |  |  | yes | yes |  |  | yes | yes | yes | yes |  |  | yes | yes |  | REFERENCE_ONLY |
| Violent-Monkey_userscripts |  |  |  |  |  |  |  |  | yes |  | yes |  | yes |  |  |  | yes |  | yes | yes |  |  | yes |  |  |  |  |  | REFERENCE_ONLY |
| Web3.swift | yes |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  |  |  |  |  | yes |  |  | yes | yes |  | yes | yes |  | IMPORT_AND_REFACTOR |
| useful-forks.github.io |  |  |  |  |  |  |  |  | yes |  |  |  | yes | yes |  |  | yes |  |  |  |  |  |  |  |  | yes | yes |  | REFERENCE_ONLY |
| vault-ai |  |  |  |  |  |  |  |  | yes |  | yes | yes | yes | yes |  | yes | yes |  |  |  |  |  |  |  | yes | yes |  |  | REFERENCE_ONLY |
| web3wizards |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| windhawk-mods | yes |  |  |  |  |  |  |  |  |  | yes |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| web3gpt |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  | yes |  |  | REFERENCE_ONLY |
| web3swift |  |  |  |  |  |  | yes |  |  |  |  |  |  | yes |  | yes | yes |  | yes | yes |  |  |  | yes |  | yes |  |  | REFERENCE_ONLY |
| img_to_ascii_converter |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| water_reminder |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  | yes | yes |  |  |  |  |  |  | REFERENCE_ONLY |
| video_to_gif |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| torrent_searcher |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| speed_test |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  |  | REFERENCE_ONLY |
| youtube_feed_details_scraper |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  | yes |  |  |  |  | yes |  |  |  | REFERENCE_ONLY |
| yt_clipper |  |  |  |  |  |  |  |  |  |  |  | yes |  | yes |  |  |  |  |  |  |  |  | yes |  |  |  |  |  | REFERENCE_ONLY |
| youtube-private-playlist-downloader |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| youtube-video-downloader |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| text-to-sound |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| autoclicker |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| battery-notification |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  | REJECT |
| yts-top-movies |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  | REJECT |
| async_port_scanner |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | REJECT |
| files_deletion |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| text Similarity |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| diff_utility |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  | REJECT |
| disk_usage |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| TAP-main |  |  |  |  |  |  |  |  | yes |  |  |  | yes | yes |  |  | yes |  |  |  |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| 00008110-000A31AA0132801E |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| spot-1.8.7 |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  |  |  | yes |  | yes | yes | yes | yes | yes | yes |  | IMPORT_AND_REFACTOR |
| spot-main |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  |  |  | yes |  | yes | yes | yes | yes | yes | yes |  | IMPORT_AND_REFACTOR |
| IPAPatch |  |  |  | yes |  |  |  |  |  |  | yes | yes | yes |  |  | yes |  |  |  |  |  |  | yes | yes |  |  | yes |  | IMPORT_AND_REFACTOR |
| Delta.app |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| IMAZING_(PROFILE-MANIFESTS) |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| 00008110-000A31AA0132801E |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| qwen-agent |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| validator |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  | yes |  |  |  |  |  | REFERENCE_ONLY |
| ecig-mafia_catalog_tool | yes |  |  |  | yes |  |  |  | yes | yes | yes |  |  | yes |  |  | yes |  | yes | yes | yes |  | yes | yes | yes | yes |  |  | IMPORT_AND_REFACTOR |
| check_weather |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| ping_checker |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| nova_master |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| OthersideAI-self-operating-computer-de256f5 |  |  |  | yes |  | yes |  |  | yes | yes | yes |  |  |  |  | yes |  |  |  | yes |  | yes | yes |  |  | yes | yes |  | REFERENCE_ONLY |
| email-clean_agent |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| InventoryPhotoOps | yes | yes | yes | yes |  |  |  |  | yes |  | yes |  |  | yes |  | yes | yes |  | yes | yes | yes | yes | yes |  |  | yes | yes | yes | IMPORT_AND_REFACTOR |
| search_news |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| keyboard-logger |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  | REJECT |
| language_translate |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| Synthetic_data_geneator |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| gender&ethnicity |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  | REJECT |
| tfHub_sentence_similarity |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| wikipedia_search |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| pomodoro_timer_gui |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  | yes |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| serial_read |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | yes |  |  |  |  |  | REJECT |
| draw_bbox_for_detection |  |  |  |  | yes | yes |  |  |  |  |  | yes |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| sorting_visualizer |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| wallpaper-changer |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  | REJECT |
| img_to_PencilSketch |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| screenshot |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| latitude_longitude |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| locate_addresses |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| webp to img |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| word_cloud_generator |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| random_fact |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | REJECT |
| scrape-Wikipedia-using-speech-recognition |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| news_scrapper |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| programming-quote |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| scrap_dark_websites |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| scrap_github_repos |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| scrap_all_email |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| scrap_all_links |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| medium_article_scraper |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| amazon-price-alert |  |  |  |  |  |  |  |  | yes |  |  |  |  | yes |  |  |  |  |  | yes | yes |  |  |  |  |  |  |  | REFERENCE_ONLY |
| check_github_username |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  | REJECT |
| qr-code-scanner |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| qr_code_generator |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| imdb-scraper |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  | yes |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| internshala_scraper |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | REJECT |
| find_brokenLinks |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| flipkart-price-alert |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| sites-central-hub |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| telegramgroupleaver |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | REJECT |
| ToM-SWE |  |  |  |  |  |  | yes |  | yes |  |  |  |  |  |  | yes | yes |  |  | yes |  |  |  |  |  | yes |  |  | REFERENCE_ONLY |
| telegram-scraper |  |  |  |  |  |  | yes |  | yes | yes | yes |  |  |  |  |  | yes |  |  | yes |  | yes | yes | yes |  |  |  |  | IMPORT_AND_REFACTOR |
| telegram-support-bot |  |  |  |  |  |  | yes |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| Windows-MCP |  |  |  | yes |  |  |  |  | yes |  |  |  |  |  |  |  | yes |  |  |  |  | yes | yes | yes |  | yes | yes |  | IMPORT_AND_REFACTOR |
| wordvis |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  | REJECT |
| tweakcc-main |  |  |  |  |  |  | yes | yes |  |  |  | yes |  |  |  |  | yes |  |  | yes |  |  | yes |  | yes | yes | yes |  | REFERENCE_ONLY |
| ultracite-ultracite-6.3.9 |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | yes | yes |  | REFERENCE_ONLY |
| table-sorter |  |  |  |  |  |  |  |  | yes |  |  |  |  | yes |  |  |  |  |  |  |  |  | yes |  |  |  |  |  | REFERENCE_ONLY |
| show-passwords |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| SimpleTweakEditor-main |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  |  |  |  |  |  | yes | yes |  | yes | yes |  | yes | yes |  | IMPORT_AND_REFACTOR |
| remix |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  | REJECT |
| save-restore |  |  |  |  |  |  |  |  | yes |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| spot |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  |  |  | yes |  | yes | yes | yes | yes | yes | yes |  | IMPORT_AND_REFACTOR |
| spot-main |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  |  |  | yes |  | yes | yes | yes | yes | yes | yes |  | IMPORT_AND_REFACTOR |
| software-agent-sdk |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  | yes |  |  |  |  |  | yes | yes |  | REFERENCE_ONLY |
| spiderfoot |  |  |  |  |  |  | yes |  | yes |  |  |  |  |  |  |  | yes |  |  | yes |  |  |  |  |  | yes | yes |  | REFERENCE_ONLY |
| awesome-ai-tools |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | REJECT |
| backup_backup |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| Appz |  | yes |  |  |  |  |  |  | yes |  |  |  |  |  |  |  | yes |  |  |  |  |  |  | yes |  | yes | yes |  | REFERENCE_ONLY |
| Assist-ER |  |  |  |  |  |  |  |  | yes |  | yes |  |  |  |  |  | yes |  | yes | yes |  | yes | yes |  |  | yes | yes |  | REFERENCE_ONLY |
| beehive | yes |  |  |  |  |  |  |  | yes |  | yes |  |  | yes |  |  | yes |  |  |  |  |  | yes |  | yes |  | yes |  | IMPORT_AND_REFACTOR |
| bgremover-app |  | yes |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  | yes | yes | yes |  |  | yes | yes |  | REFERENCE_ONLY |
| bark |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  | yes | yes |  |  | yes | yes |  |  |  |  | yes |  |  | REFERENCE_ONLY |
| Beebz |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| AnyLint |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  | yes |  |  | yes | yes |  |  | yes | yes |  | yes | yes |  | IMPORT_AND_REFACTOR |
| agent-starter-pack |  | yes |  |  |  |  |  |  | yes | yes |  |  | yes | yes |  |  | yes |  |  | yes |  |  |  |  |  | yes |  | yes | IMPORT_AND_REFACTOR |
| AGENT_K |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  | yes |  | yes | yes | yes |  | yes | yes |  | yes | IMPORT_AND_REFACTOR |
| wren-coder-cli |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  |  | yes |  |  | yes | yes | yes | REFERENCE_ONLY |
| 940smiley.github.io |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| Amazing-Python-Scripts |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  | yes |  |  |  |  |  |  | REJECT |
| Amazon_Nova_Agent_on_GIT |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| ai-agents-for-beginners | yes |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| AltDeploy |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  | yes |  |  | REJECT |
| qwen-code |  |  |  | yes |  |  | yes |  |  |  |  |  | yes |  |  | yes | yes |  |  | yes |  | yes | yes |  |  | yes | yes | yes | REFERENCE_ONLY |
| CrossCode-main |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  | yes | yes | yes |  | yes | yes |  | REFERENCE_ONLY |
| easylist-master |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  | yes | REFERENCE_ONLY |
| clipper |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  | REJECT |
| CrossCode-0.0.5 |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  | yes | yes | yes |  | yes | yes |  | REFERENCE_ONLY |
| giveawonderfulday |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| GLM-4.5-main |  |  |  |  |  |  | yes |  | yes |  |  |  | yes | yes |  |  | yes |  |  |  |  |  |  | yes |  | yes | yes |  | REFERENCE_ONLY |
| Fire-Scripts-CLI |  |  |  |  |  |  |  |  |  |  |  |  | yes | yes |  |  |  |  |  |  |  |  | yes |  | yes |  | yes |  | REFERENCE_ONLY |
| github-app-js-sample |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| claude-task-master-main |  |  |  |  |  |  |  | yes | yes |  |  |  | yes |  |  |  | yes |  |  |  |  |  | yes |  |  | yes | yes |  | REFERENCE_ONLY |
| aider |  |  |  |  |  |  |  |  | yes |  | yes |  |  |  |  |  | yes |  |  | yes |  |  | yes | yes | yes | yes | yes |  | REFERENCE_ONLY |
| animus-coder |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  | REFERENCE_ONLY |
| AddCurrentPath-main |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  | REJECT |
| agentpipe |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  | yes |  |  | yes | yes |  |  | yes |  |  | yes | yes | yes | REFERENCE_ONLY |
| bard-powered-telegram-bot |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  |  |  |  | REFERENCE_ONLY |
| blockchain-master |  |  |  |  |  |  |  |  | yes | yes |  |  |  | yes |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| APT |  | yes |  |  |  |  |  |  | yes |  |  |  | yes |  |  |  | yes |  |  | yes | yes |  | yes | yes | yes | yes | yes |  | IMPORT_AND_REFACTOR |
| autogen |  |  |  |  |  |  | yes |  |  |  |  |  |  | yes |  |  | yes |  |  | yes |  |  |  | yes |  | yes | yes |  | REFERENCE_ONLY |
| openclaw | yes |  |  | yes |  |  | yes |  | yes |  |  |  |  |  |  | yes | yes |  |  | yes |  | yes | yes | yes | yes | yes | yes | yes | REFERENCE_ONLY |
| opendan-personal-ai-os |  |  |  |  | yes |  | yes |  |  |  |  |  | yes |  |  |  |  |  |  | yes |  |  | yes | yes |  | yes | yes |  | REFERENCE_ONLY |
| mtkbrute |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  | yes | yes | REFERENCE_ONLY |
| nexus-layer |  |  |  | yes |  |  | yes |  | yes |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| openhands-chrome-extension |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  | yes |  | yes |  |  |  |  |  | REFERENCE_ONLY |
| OpenHands-CLI |  |  |  |  |  |  | yes |  | yes |  | yes |  |  |  |  |  | yes |  |  | yes |  |  |  |  |  | yes | yes |  | REFERENCE_ONLY |
| OpenHands |  |  |  |  |  |  | yes |  | yes | yes |  |  |  | yes |  | yes | yes |  |  | yes | yes |  |  |  |  | yes |  | yes | REFERENCE_ONLY |
| OpenHands-1.0.1-cli |  |  |  |  |  |  |  |  | yes | yes |  |  | yes | yes |  | yes | yes |  |  | yes | yes |  |  |  |  | yes |  | yes | REFERENCE_ONLY |
| monkeyshine |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  | yes | yes |  |  | REFERENCE_ONLY |
| hosts |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  | yes |  |  | yes |  |  |  |  |  | yes | yes |  | REFERENCE_ONLY |
| iLEAPP-main |  |  |  |  |  |  |  |  | yes |  | yes |  | yes | yes |  | yes | yes |  |  | yes |  |  | yes | yes | yes | yes | yes |  | IMPORT_AND_REFACTOR |
| goose-1.23.0 |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  | yes | yes |  |  | yes | yes | yes | REFERENCE_ONLY |
| healing-agent-main |  |  |  |  |  |  |  |  | yes |  |  | yes | yes |  |  |  | yes |  |  | yes |  |  | yes |  |  | yes |  | yes | REFERENCE_ONLY |
| localagi |  |  |  | yes |  |  | yes |  | yes |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  | yes | yes | yes | REFERENCE_ONLY |
| localai |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  | yes | yes |  | REFERENCE_ONLY |
| intelligence-hub |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  | yes | yes | yes |  | yes |  | REFERENCE_ONLY |
| Kimi-K2.5 |  |  |  | yes |  |  | yes |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| Bootstrap |  |  |  |  |  |  | yes |  | yes | yes |  |  |  | yes |  | yes |  |  | yes |  | yes |  | yes | yes |  |  | yes | yes | REFERENCE_ONLY |
| mintmuseily |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| models |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  |  | yes |  |  |  |  |  | yes | yes |  | REFERENCE_ONLY |
| mcp-hubspot |  |  |  |  |  |  |  |  | yes | yes |  |  |  | yes |  |  | yes |  |  |  |  | yes | yes |  |  | yes |  |  | REFERENCE_ONLY |
| Meshroom |  | yes |  | yes |  |  | yes |  |  |  |  |  |  | yes |  | yes | yes |  | yes |  |  |  |  | yes |  | yes | yes |  | IMPORT_AND_REFACTOR |
| oclint |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| openai-assistant-swarm | yes |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  | yes |  |  | yes |  |  | yes |  | yes | yes |  |  | REFERENCE_ONLY |
| mosaic |  | yes |  |  |  |  |  |  |  |  |  |  |  | yes |  | yes |  |  |  |  | yes |  | yes |  |  | yes |  |  | REFERENCE_ONLY |
| nextjs-subscription-payments |  |  |  |  |  |  |  | yes |  |  |  |  | yes | yes |  |  | yes |  |  | yes |  |  |  |  |  | yes | yes |  | REFERENCE_ONLY |
| Logical_Reasoning_Authoritive_Agent |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| jessgeterdone |  | yes |  |  |  |  |  |  | yes |  |  |  |  | yes |  |  | yes |  |  |  |  |  |  |  | yes |  |  |  | REFERENCE_ONLY |
| libimobiledevice.org |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  | yes |  | yes |  |  | yes |  | REFERENCE_ONLY |
| Inventory_Photos_-_Documents |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| ipwndfu_public |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  |  |  |  |  |  | yes |  |  | yes | yes |  | yes | yes |  | REFERENCE_ONLY |
| lmstudio-js |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes | yes |  |  | yes |  |  |  |  | yes | yes | yes |  | REFERENCE_ONLY |
| lmstudio-python | yes |  |  |  |  |  | yes | yes | yes |  |  |  |  |  |  | yes | yes |  |  | yes |  |  |  |  |  | yes | yes |  | IMPORT_AND_REFACTOR |
| libirecovery |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  | yes | yes |  |  |  |  | REFERENCE_ONLY |
| litellm |  |  |  |  |  |  | yes |  | yes | yes | yes |  | yes |  |  |  | yes |  |  | yes | yes |  |  |  |  | yes | yes | yes | REFERENCE_ONLY |
| regex_parser |  |  |  |  |  |  |  |  | yes |  |  |  |  | yes |  |  |  |  |  |  |  |  | yes |  | yes | yes |  |  | REFERENCE_ONLY |
| RepoOrchestrator |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  |  |  |  |  | yes |  | yes | REFERENCE_ONLY |
| Recoveredtreasures_ebay_manager | yes |  |  |  |  |  |  |  | yes |  | yes |  | yes | yes |  |  | yes |  |  |  | yes |  |  |  |  | yes | yes |  | REFERENCE_ONLY |
| recovered_treasures_app | yes |  |  |  |  |  |  |  | yes |  | yes |  | yes | yes |  |  | yes |  |  |  | yes |  |  |  |  | yes | yes |  | REFERENCE_ONLY |
| Side-Store-Altered-Pro |  |  |  |  |  |  | yes |  |  |  |  |  | yes |  |  | yes | yes |  |  |  |  | yes | yes | yes |  | yes |  |  | IMPORT_AND_REFACTOR |
| Sideloader |  |  |  |  |  |  | yes |  | yes |  |  | yes | yes |  |  | yes | yes |  |  |  |  |  | yes | yes | yes | yes | yes | yes | IMPORT_AND_REFACTOR |
| ReVens |  |  |  |  |  |  |  |  | yes | yes | yes |  | yes | yes |  |  |  |  | yes | yes | yes | yes | yes |  |  | yes | yes |  | REFERENCE_ONLY |
| self-hosted-ai-starter-kit |  |  |  |  |  |  | yes |  |  |  |  |  | yes | yes |  | yes |  |  |  | yes |  |  |  | yes |  | yes |  |  | REFERENCE_ONLY |
| RecoveredTreasuresTX.shop-Website-files | yes | yes |  | yes |  |  |  | yes | yes |  |  |  |  |  |  | yes | yes |  | yes |  |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| Password-Checker |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  | REFERENCE_ONLY |
| PERS-Chat-GPT-Backup |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| OpsToolkit-v1 |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  | yes | yes |  | yes |  |  |  | yes | yes | yes | REFERENCE_ONLY |
| Org-Brain |  |  |  |  |  |  |  |  |  |  | yes |  |  | yes |  |  | yes |  |  | yes |  | yes |  |  | yes | yes |  |  | REFERENCE_ONLY |
| reading-list-mover |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  | yes |  |  | yes |  |  | yes |  |  |  |  |  | REFERENCE_ONLY |
| recover-log-list |  |  |  | yes |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  |  | yes |  |  | yes | yes |  |  | yes | yes | IMPORT_AND_REFACTOR |
| Pythonista |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| quickstart-ios |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  | yes |  | yes |  | yes | REFERENCE_ONLY |
| injectionforxcode |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  |  | yes |  |  |  |  |  |  | yes | yes |  | yes |  |  | REFERENCE_ONLY |
| cursor-free-vip |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes | yes |  | yes | yes |  | REFERENCE_ONLY |
| customized-coding-companion |  |  |  | yes |  |  |  |  | yes |  |  |  | yes | yes |  |  | yes |  | yes |  |  |  | yes |  |  | yes | yes |  | IMPORT_AND_REFACTOR |
| collectivities |  |  |  | yes | yes |  |  |  | yes |  | yes |  | yes |  |  |  | yes |  |  |  |  |  |  |  | yes | yes |  | yes | REFERENCE_ONLY |
| Collectivities-DApp |  |  |  | yes | yes |  |  |  | yes |  | yes |  | yes |  |  |  | yes |  |  |  |  |  |  |  | yes | yes |  | yes | REFERENCE_ONLY |
| eBay-View-Bot |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| ebay_inventory_backup | yes |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| deliberate-agentic-development |  |  |  | yes |  |  |  |  | yes |  |  |  | yes |  |  |  |  |  | yes | yes | yes |  | yes | yes |  | yes | yes | yes | REFERENCE_ONLY |
| DeV-AI-PRO |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| COLLECTIBLE_AI_ |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  | REJECT |
| chatbot |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  | yes | yes |  |  |  |  |  |  |  |  | yes | yes |  | REFERENCE_ONLY |
| ChatChat |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  |  |  |  |  |  |  |  | REJECT |
| cardops | yes | yes | yes | yes | yes |  | yes |  | yes | yes |  | yes | yes | yes |  | yes | yes |  | yes | yes | yes |  | yes |  |  | yes | yes | yes | REFERENCE_ONLY |
| Chat-GPT_PortPilot |  |  |  |  |  |  |  |  | yes |  | yes | yes |  |  |  |  | yes |  |  | yes |  |  |  |  |  |  |  | yes | REFERENCE_ONLY |
| Chatgpt_connector_app |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| Collectease |  |  |  |  |  |  |  |  |  |  |  |  | yes | yes | yes | yes | yes |  |  | yes |  |  |  |  |  |  |  |  | REFERENCE_ONLY |
| chatgpt-omnibox |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | REJECT |
| chatgptjs |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  | REJECT |
| GoogleSheetsCMS |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes | yes | REJECT |
| gptbot |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  | yes | yes |  |  |  |  |  |  |  |  | yes | yes |  | REFERENCE_ONLY |
| gitty-gitty-git-er |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  | yes |  | yes |  | yes |  |  | REFERENCE_ONLY |
| Give-A-Wonderful-Day |  |  |  |  |  |  |  |  | yes |  | yes |  |  | yes |  |  |  |  | yes |  |  |  |  |  | yes | yes | yes |  | REFERENCE_ONLY |
| image_augmentor |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  | yes | yes |  |  |  |  |  | yes |  |  |  |  |  | REFERENCE_ONLY |
| improved-dollop |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  | REFERENCE_ONLY |
| hydrogen |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  | yes |  |  | yes |  |  |  |  |  | yes |  |  | REFERENCE_ONLY |
| image-pro-cacaws_copy | yes |  |  |  |  |  |  |  | yes |  | yes | yes |  | yes |  |  | yes |  |  | yes |  | yes | yes | yes |  | yes | yes |  | REFERENCE_ONLY |
| Git-Go |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | REJECT |
| EthereumKit |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  |  |  | yes |  |  | yes |  | REFERENCE_ONLY |
| ExplorerPatcher |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  | yes |  |  |  | yes |  | REFERENCE_ONLY |
| EBAY_INVENTORY_PHOTO_ORGANIZER | yes | yes |  | yes | yes |  |  |  | yes | yes |  |  | yes |  |  |  | yes |  |  |  | yes |  |  | yes |  |  |  |  | IMPORT_AND_REFACTOR |
| ecommerce_autolister |  |  |  |  |  |  |  |  | yes |  | yes |  |  |  |  | yes | yes |  |  | yes |  | yes |  |  |  |  | yes |  | REFERENCE_ONLY |
| fundme |  |  |  |  |  |  |  |  | yes |  |  |  | yes |  |  |  |  |  |  |  |  | yes |  |  | yes |  |  |  | REFERENCE_ONLY |
| generative-patterns |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  | REJECT |
| FOUNDRY-CENTRIC-local-AI-GUI |  |  |  |  |  |  |  |  | yes |  |  |  |  |  |  |  | yes |  |  |  |  | yes | yes | yes |  |  | yes |  | REFERENCE_ONLY |
| freedom-fleamarket-biz-production |  |  |  | yes |  |  |  |  | yes |  |  |  |  |  |  |  |  |  |  |  |  |  | yes |  |  |  |  |  | REFERENCE_ONLY |
