import React, { useState, useEffect, useRef } from 'react';
import { 
  LayoutDashboard, 
  ListFilter, 
  Image as ImageIcon, 
  Sparkles, 
  Scissors, 
  FolderSearch, 
  Share2, 
  QrCode, 
  RefreshCw, 
  CheckCircle, 
  AlertCircle, 
  Upload, 
  Info,
  Maximize2, 
  RotateCw, 
  Crop, 
  Palette, 
  ExternalLink,
  Compass,
  Check,
  TrendingUp,
  Plug
} from 'lucide-react';
import confetti from 'canvas-confetti';
import EbayOAuthPanel from './components/EbayOAuthPanel.jsx';

// Real-world Mock Categories
const EBAY_CATEGORIES = [
  { id: 261328, name: "Collectible Card Games > Pokémon TCG" },
  { id: 1834, name: "Stamps > United States > Historical" },
  { id: 63, name: "Comics > Golden Age (1938-55)" },
  { id: 11112, name: "Coins & Paper Money > US Coins" },
  { id: 260, name: "Stamps > Worldwide" },
  { id: 261330, name: "Collectible Card Games > Magic: The Gathering" }
];

export default function App() {
  const [activeTab, setActiveTab] = useState('dashboard');
  const [logs, setLogs] = useState([
    { id: 1, type: 'system', text: 'eBay Hero Control Center initialized.' },
    { id: 2, type: 'success', text: 'SQLite Database connected. 826 photo references indexed.' }
  ]);
  const [pairedDevice, setPairedDevice] = useState(null);
  const [isPairing, setIsPairing] = useState(false);
  const [trainingStatus, setTrainingStatus] = useState("Idle (Ready for corrections)");
  const [ebayStatus, setEbayStatus] = useState('disconnected'); // 'connected' | 'disconnected'

  // Main Inventory State (Simulating consolidated SQLite data)
  const [inventory, setInventory] = useState([
    {
      id: "cardops-card-1",
      title: "1999 Pokémon Base Set Shadowless Pikachu #58/102",
      category: "CCG Pokémon Cards",
      categoryId: 261328,
      price: 349.99,
      format: "Buy It Now",
      status: "Currently Listed",
      listedDate: "2026-07-10",
      confidence: 0.98,
      thumbnail: "https://images.unsplash.com/photo-1607604276583-eef5d076aa5f?w=150&auto=format&fit=crop&q=60",
      details: "Shadowless Red Cheeks, Near Mint condition."
    },
    {
      id: "cardops-card-2",
      title: "1894 George Washington 2-Cent Red Stamp (Used, No Gum)",
      category: "US Stamps",
      categoryId: 1834,
      price: 45.00,
      format: "Auction",
      status: "Currently Listed",
      listedDate: "2026-07-15",
      confidence: 0.92,
      thumbnail: "https://images.unsplash.com/photo-1579546929518-9e396f3cc809?w=150&auto=format&fit=crop&q=60",
      details: "Triangles in corners, postmarked, no gum."
    },
    {
      id: "cardops-card-3",
      title: "Action Comics #1 Replica Reprint (1998)",
      category: "Comics",
      categoryId: 63,
      price: 120.00,
      format: "Buy It Now",
      status: "Never Listed",
      listedDate: null,
      confidence: 0.95,
      thumbnail: "https://images.unsplash.com/photo-1612036782180-6f0b6cd846fe?w=150&auto=format&fit=crop&q=60",
      details: "Celebration reprint edition, mint condition bag & board."
    },
    {
      id: "cardops-card-4",
      title: "1888 Morgan Silver Dollar Coin - Philadelphia Mint",
      category: "US Coins",
      categoryId: 11112,
      price: 55.00,
      format: "Buy It Now",
      status: "Listed within 30 days",
      listedDate: "2026-07-01",
      confidence: 0.89,
      thumbnail: "https://images.unsplash.com/photo-1621972750749-0fbb1abb7736?w=150&auto=format&fit=crop&q=60",
      details: "Fine detail, nice silver luster."
    }
  ]);

  // Ingestion Queue State for AI Classification Workspace
  const [ingestQueue, setIngestQueue] = useState([
    {
      id: "ingest-1",
      image: "https://images.unsplash.com/photo-1568849676085-51415703900f?w=400&auto=format&fit=crop&q=60",
      detectedObject: "Trading Card Game Card",
      suggestedTitle: "2000 Neo Genesis Lugia Holo #9",
      suggestedCategory: "CCG Pokémon Cards",
      suggestedCategoryId: 261328,
      confidence: 0.84,
      isCorrected: false,
      bbox: { x: 50, y: 30, w: 300, h: 420 }
    },
    {
      id: "ingest-2",
      image: "https://images.unsplash.com/photo-1544816155-12df9643f363?w=400&auto=format&fit=crop&q=60",
      detectedObject: "Collectible Stamp",
      suggestedTitle: "1923 US Warren Harding 2c Black Memorial Stamp",
      suggestedCategory: "US Stamps",
      suggestedCategoryId: 1834,
      confidence: 0.91,
      isCorrected: false,
      bbox: { x: 80, y: 60, w: 240, h: 280 }
    }
  ]);

  const [selectedQueueItem, setSelectedQueueItem] = useState(ingestQueue[0]);

  // Image Editor State
  const [editImage, setEditImage] = useState(inventory[0].thumbnail);
  const [rotation, setRotation] = useState(0);
  const [cropActive, setCropActive] = useState(false);
  const [bgRemoved, setBgRemoved] = useState(false);
  const [isRemovingBg, setIsRemovingBg] = useState(false);
  const canvasRef = useRef(null);

  // Search & Filter State
  const [searchTerm, setSearchTerm] = useState('');
  const [categoryFilter, setCategoryFilter] = useState('All');
  const [statusFilter, setStatusFilter] = useState('All');
  const [formatFilter, setFormatFilter] = useState('All');

  // Seed Visual Sorting State
  const [seeds, setSeeds] = useState([
    { id: 'seed-pokemon', name: 'Pokémon Card Seed', desc: 'CCG Pokemon Cards', sample: 'https://images.unsplash.com/photo-1607604276583-eef5d076aa5f?w=80&auto=format&fit=crop&q=60' },
    { id: 'seed-stamp', name: 'US Stamps Seed', desc: '19th century historical stamps', sample: 'https://images.unsplash.com/photo-1579546929518-9e396f3cc809?w=80&auto=format&fit=crop&q=60' }
  ]);
  const [sortProgress, setSortProgress] = useState(null);

  // Social Sharing configuration
  const [socialAccounts, setSocialAccounts] = useState({
    facebook: true,
    instagram: false,
    twitter: true
  });
  const [shareStatus, setShareStatus] = useState(null);

  // Add Log Helper
  const addLog = (text, type = 'system') => {
    setLogs(prev => [{ id: Date.now(), type, text }, ...prev]);
  };

  // Simulate mobile camera ingest pairing
  const handlePairDevice = () => {
    setIsPairing(true);
    addLog("Generated mobile sync pair token. Waiting for device scan...", "system");
    setTimeout(() => {
      setPairedDevice({ name: "iPhone 15 Pro", ip: "192.168.1.84" });
      setIsPairing(false);
      addLog("Paired mobile client: iPhone 15 Pro successfully connected.", "success");
    }, 3000);
  };

  const handleSimulateMobileUpload = () => {
    if (!pairedDevice) return;
    addLog("Mobile camera ingest: Received new image stream.", "system");
    
    // Add a new raw image to ingestion queue
    const isCard = Math.random() > 0.5;
    const newItem = {
      id: `ingest-${Date.now()}`,
      image: isCard 
        ? "https://images.unsplash.com/photo-1568849676085-51415703900f?w=400&auto=format&fit=crop&q=60" 
        : "https://images.unsplash.com/photo-1579783900882-c0d3dad7b119?w=400&auto=format&fit=crop&q=60",
      detectedObject: isCard ? "Trading Card" : "Collectible Stamp",
      suggestedTitle: isCard ? "2002 Pokémon Expedition Mewtwo Holo" : "1908 Benjamin Franklin 1c Green Stamp",
      suggestedCategory: isCard ? "CCG Pokémon Cards" : "US Stamps",
      suggestedCategoryId: isCard ? 261328 : 1834,
      confidence: 0.88,
      isCorrected: false,
      bbox: { x: 40, y: 40, w: 320, h: 400 }
    };
    
    setIngestQueue(prev => [newItem, ...prev]);
    setSelectedQueueItem(newItem);
    addLog(`AI Object Detection: Identified ${newItem.detectedObject} (Confidence: ${newItem.confidence * 100}%)`, "success");
  };

  // AI Categorization Correction Loop
  const handleUpdateAndTrain = (correctedTitle, correctedCategory, catId) => {
    if (!selectedQueueItem) return;
    
    setTrainingStatus("Training model in background...");
    addLog(`User submitted correction: "${correctedTitle}" categorized as ${correctedCategory}.`, "system");
    
    setTimeout(() => {
      // Create new inventory item
      const newItem = {
        id: `cardops-card-${Date.now()}`,
        title: correctedTitle,
        category: correctedCategory,
        categoryId: parseInt(catId),
        price: correctedCategory.includes("Pokémon") ? 180.00 : 35.00,
        format: "Buy It Now",
        status: "Never Listed",
        listedDate: null,
        confidence: 1.0, // User confirmed
        thumbnail: selectedQueueItem.image,
        details: "AI Model updated dynamically. Manually verified."
      };
      
      setInventory(prev => [newItem, ...prev]);
      setIngestQueue(prev => prev.filter(item => item.id !== selectedQueueItem.id));
      setSelectedQueueItem(null);
      setTrainingStatus("Model updated successfully with new feedback seed.");
      addLog(`AI training complete. Database catalog updated.`, "success");
      confetti({ particleCount: 80, spread: 60 });
    }, 2000);
  };

  // Seed Visual Sorting simulation
  const handleRunSeedSort = () => {
    setSortProgress("Analyzing image signatures...");
    addLog("Visual Similarity Sorter triggered using active seed templates...", "system");
    setTimeout(() => {
      setSortProgress("Sorting files...");
      setTimeout(() => {
        setSortProgress(null);
        addLog("Batch visual sort finished: Classified 8 unassigned images matching Pokémon TCG templates.", "success");
        addLog("Batch visual sort finished: Classified 3 images matching US Stamps templates.", "success");
        confetti({ particleCount: 100, spread: 80, colors: ['#00f2fe', '#8a2be2'] });
      }, 1500);
    }, 1500);
  };

  // Image Editor Canvas Implementation
  useEffect(() => {
    const canvas = canvasRef.current;
    if (!canvas) return;
    const ctx = canvas.getContext('2d');
    const img = new Image();
    img.crossOrigin = "anonymous";
    img.src = editImage;
    img.onload = () => {
      canvas.width = 300;
      canvas.height = 300;
      ctx.clearRect(0, 0, canvas.width, canvas.height);
      ctx.save();
      ctx.translate(canvas.width / 2, canvas.height / 2);
      ctx.rotate((rotation * Math.PI) / 180);
      ctx.drawImage(img, -100, -100, 200, 200);
      ctx.restore();
    };
  }, [editImage, rotation, bgRemoved]);

  const handleRemoveBg = () => {
    setIsRemovingBg(true);
    addLog("Background removal agent running...", "system");
    setTimeout(() => {
      setBgRemoved(true);
      setIsRemovingBg(false);
      addLog("Background removed. Redrawn foreground object canvas.", "success");
    }, 2000);
  };

  // Optimize Listing check
  const handleOptimize = (item) => {
    addLog(`Running eBay lister optimizer for: ${item.title}`, "system");
    setTimeout(() => {
      // Modify title slightly for maximum search indexing
      setInventory(prev => prev.map(inv => {
        if (inv.id === item.id) {
          return {
            ...inv,
            title: inv.title + " PSA Mint CCG",
            details: inv.details + " Optimized title for better eBay search ranking."
          };
        }
        return inv;
      }));
      addLog(`Optimization complete: Added high-volume keywords to title.`, "success");
      confetti({ particleCount: 50, spread: 40 });
    }, 1000);
  };

  // Social Share simulation
  const handleSocialShare = (item) => {
    setShareStatus({ item, channels: [] });
  };

  const executeShare = () => {
    addLog(`Sharing listing "${shareStatus.item.title}" to configured social networks...`, "system");
    setTimeout(() => {
      setShareStatus(null);
      addLog("Social broadcast successful!", "success");
      confetti({ particleCount: 60, colors: ['#ffd700', '#2ed573'] });
    }, 1500);
  };

  // Filters application
  const filteredInventory = inventory.filter(item => {
    const matchesSearch = item.title.toLowerCase().includes(searchTerm.toLowerCase()) || 
                          item.id.toLowerCase().includes(searchTerm.toLowerCase());
    const matchesCategory = categoryFilter === 'All' || item.category.toLowerCase().includes(categoryFilter.toLowerCase());
    const matchesStatus = statusFilter === 'All' || 
                          (statusFilter === 'Currently Listed' && item.status === 'Currently Listed') ||
                          (statusFilter === 'Listed within 30 days' && item.status === 'Listed within 30 days') ||
                          (statusFilter === 'Never Listed' && item.status === 'Never Listed');
    const matchesFormat = formatFilter === 'All' || item.format === formatFilter;
    
    return matchesSearch && matchesCategory && matchesStatus && matchesFormat;
  });

  return (
    <div className="app-container" style={{ display: 'flex', minHeight: '100vh', flexDirection: 'column' }}>
      
      {/* GLOWING HEADER */}
      <header className="glass" style={{ margin: '16px', padding: '16px 24px', display: 'flex', justifyContent: 'space-between', alignItems: 'center', zIndex: 100 }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <div style={{ background: 'linear-gradient(135deg, var(--accent-secondary), var(--accent-primary))', width: '40px', height: '40px', borderRadius: '10px', display: 'flex', alignItems: 'center', justifyContent: 'center', fontWeight: 'bold', fontSize: '20px', boxShadow: '0 0 15px rgba(0,242,254,0.4)' }}>
            ⚡
          </div>
          <div>
            <h1 style={{ fontSize: '22px', fontWeight: '800', lineHeight: 1.1 }}>eBay Hero</h1>
            <span style={{ fontSize: '11px', color: 'var(--accent-secondary)', fontWeight: '600', letterSpacing: '0.05em', textTransform: 'uppercase' }}>Command Center</span>
          </div>
        </div>
        
        {/* Connection States */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '16px', fontSize: '13px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '6px', background: 'rgba(46,59,78,0.4)', padding: '6px 12px', borderRadius: '20px' }}>
            <span style={{ width: '8px', height: '8px', borderRadius: '50%', backgroundColor: 'var(--accent-success)', display: 'inline-block' }}></span>
            <span>eBay Sandbox connected</span>
          </div>
          {pairedDevice ? (
            <div style={{ display: 'flex', alignItems: 'center', gap: '6px', background: 'rgba(0,242,254,0.1)', border: '1px solid rgba(0,242,254,0.3)', padding: '6px 12px', borderRadius: '20px' }}>
              <span style={{ fontSize: '11px' }}>📱 Sync: {pairedDevice.name}</span>
            </div>
          ) : (
            <button onClick={handlePairDevice} style={{ background: 'rgba(255,255,255,0.08)', border: 'none', padding: '6px 12px', borderRadius: '20px', cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '4px' }}>
              <QrCode size={14} /> Connect Phone
            </button>
          )}
        </div>
      </header>

      {/* MAIN CONTAINER */}
      <div style={{ display: 'grid', gridTemplateColumns: '260px 1fr', flex: 1, padding: '0 16px 16px 16px', gap: '16px', height: 'calc(100vh - 120px)' }}>
        
        {/* SIDE NAV */}
        <aside className="glass" style={{ padding: '16px', display: 'flex', flexDirection: 'column', gap: '8px' }}>
          <h4 style={{ fontSize: '11px', textTransform: 'uppercase', color: 'var(--text-muted)', margin: '8px 0 8px 12px', letterSpacing: '0.1em' }}>Navigation</h4>
          
          <button onClick={() => setActiveTab('dashboard')} className={`nav-btn ${activeTab === 'dashboard' ? 'active' : ''}`}>
            <LayoutDashboard size={18} /> Dashboard
          </button>
          <button onClick={() => setActiveTab('inventory')} className={`nav-btn ${activeTab === 'inventory' ? 'active' : ''}`}>
            <ListFilter size={18} /> Inventory Log
          </button>
          <button onClick={() => setActiveTab('ingest')} className={`nav-btn ${activeTab === 'ingest' ? 'active' : ''}`}>
            <Sparkles size={18} /> AI Import Review
          </button>
          <button onClick={() => setActiveTab('sorter')} className={`nav-btn ${activeTab === 'sorter' ? 'active' : ''}`}>
            <ImageIcon size={18} /> Seed Sorting
          </button>
          <button onClick={() => setActiveTab('editor')} className={`nav-btn ${activeTab === 'editor' ? 'active' : ''}`}>
            <Scissors size={18} /> Image Editor
          </button>
          <button onClick={() => setActiveTab('categories')} className={`nav-btn ${activeTab === 'categories' ? 'active' : ''}`}>
            <Compass size={18} /> eBay Categories
          </button>
          <button onClick={() => setActiveTab('social')} className={`nav-btn ${activeTab === 'social' ? 'active' : ''}`}>
            <Share2 size={18} /> Social Media
          </button>
          <button onClick={() => setActiveTab('connect')} className={`nav-btn ${activeTab === 'connect' ? 'active' : ''}`} style={{ position: 'relative' }}>
            <Plug size={18} /> eBay Connect
            {ebayStatus === 'connected' && (
              <span style={{ position: 'absolute', right: '10px', width: '8px', height: '8px', borderRadius: '50%', background: 'var(--accent-success)', boxShadow: '0 0 6px var(--accent-success)' }} />
            )}
          </button>

          <div style={{ marginTop: 'auto', padding: '12px', background: 'rgba(255,255,255,0.03)', borderRadius: '8px', fontSize: '12px' }}>
            <div style={{ fontWeight: 'bold', color: 'var(--accent-secondary)' }}>AI Sync Status</div>
            <div style={{ color: 'var(--text-muted)', fontSize: '10px' }}>{trainingStatus}</div>
          </div>
        </aside>

        {/* WORKSPACE */}
        <main className="glass" style={{ padding: '24px', overflowY: 'auto', position: 'relative' }}>
          
          {/* TAB 1: DASHBOARD */}
          {activeTab === 'dashboard' && (
            <div className="animate-slide" style={{ display: 'flex', flexDirection: 'column', gap: '24px' }}>
              <div>
                <h2>Overview Dashboard</h2>
                <p style={{ color: 'var(--text-muted)' }}>Welcome to the eBay Hero command center. Monitor AI ingestion streams and manage listing states.</p>
              </div>

              {/* STATS */}
              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(200px, 1fr))', gap: '16px' }}>
                <div className="stat-card">
                  <div className="stat-label">Total Photos Indexed</div>
                  <div className="stat-value">826</div>
                  <div className="stat-sub">From InventoryPhotoOps DB</div>
                </div>
                <div className="stat-card">
                  <div className="stat-label">Currently Listed</div>
                  <div className="stat-value">542</div>
                  <div className="stat-sub">Live on eBay Sandbox</div>
                </div>
                <div className="stat-card">
                  <div className="stat-label">Unclassified Queue</div>
                  <div className="stat-value">{ingestQueue.length}</div>
                  <div className="stat-sub">Awaiting AI/Manual review</div>
                </div>
                <div className="stat-card">
                  <div className="stat-label">Model Confidence</div>
                  <div className="stat-value">94.8%</div>
                  <div className="stat-sub">Dynamic learning active</div>
                </div>
              </div>

              {/* DASHBOARD GRID */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr 340px', gap: '16px', marginTop: '16px' }}>
                {/* AI Log Activity Feed */}
                <div style={{ background: 'rgba(0,0,0,0.2)', padding: '20px', borderRadius: '12px', display: 'flex', flexDirection: 'column', gap: '12px' }}>
                  <h3 style={{ fontSize: '16px', borderBottom: '1px solid rgba(255,255,255,0.08)', paddingBottom: '8px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                    <span>AI Activity Stream</span>
                    <RefreshCw size={14} className="spinning" />
                  </h3>
                  <div style={{ display: 'flex', flexDirection: 'column', gap: '8px', maxHeight: '200px', overflowY: 'auto' }}>
                    {logs.map(log => (
                      <div key={log.id} style={{ display: 'flex', gap: '8px', fontSize: '13px', padding: '6px', background: 'rgba(255,255,255,0.02)', borderRadius: '4px' }}>
                        <span style={{ color: log.type === 'success' ? 'var(--accent-success)' : 'var(--accent-secondary)' }}>•</span>
                        <span style={{ flex: 1 }}>{log.text}</span>
                      </div>
                    ))}
                  </div>
                </div>

                {/* Mobile Camera Ingestion Hub */}
                <div style={{ background: 'rgba(0,0,0,0.2)', padding: '20px', borderRadius: '12px', display: 'flex', flexDirection: 'column', gap: '12px' }}>
                  <h3 style={{ fontSize: '16px', display: 'flex', alignItems: 'center', gap: '8px' }}><QrCode size={18} /> Phone Sync Hub</h3>
                  {isPairing ? (
                    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', padding: '20px', gap: '12px' }}>
                      <RefreshCw className="spinning" size={32} style={{ color: 'var(--accent-secondary)' }} />
                      <span style={{ fontSize: '13px' }}>Generating Sync Pairing Token...</span>
                    </div>
                  ) : pairedDevice ? (
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', textAlign: 'center', padding: '16px 0' }}>
                      <div style={{ color: 'var(--accent-success)', fontWeight: 'bold' }}>✓ Paired & Connected</div>
                      <div style={{ fontSize: '12px', color: 'var(--text-muted)' }}>Device Name: {pairedDevice.name}</div>
                      <button onClick={handleSimulateMobileUpload} className="glowing-button" style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px', padding: '10px' }}>
                        <Upload size={16} /> Simulate Ingest from Phone
                      </button>
                    </div>
                  ) : (
                    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '12px' }}>
                      <div style={{ background: '#fff', padding: '8px', borderRadius: '8px', display: 'inline-block' }}>
                        {/* Mock QR Code representation */}
                        <div style={{ width: '120px', height: '120px', background: 'repeating-conic-gradient(#000 0% 25%, #fff 0% 50%) 50% / 20px 20px', borderRadius: '4px' }}></div>
                      </div>
                      <p style={{ fontSize: '11px', textAlign: 'center', color: 'var(--text-muted)' }}>Scan with your phone camera to pair and upload items instantly without installation.</p>
                      <button onClick={handlePairDevice} style={{ border: '1px solid var(--accent-secondary)', color: 'var(--accent-secondary)', background: 'transparent', padding: '8px 16px', borderRadius: '8px', cursor: 'pointer', fontWeight: '600' }}>Simulate QR Pair Scan</button>
                    </div>
                  )}
                </div>
              </div>
            </div>
          )}

          {/* TAB 2: INVENTORY LOG */}
          {activeTab === 'inventory' && (
            <div className="animate-slide" style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
              <div>
                <h2>Full Inventory Log</h2>
                <p style={{ color: 'var(--text-muted)' }}>Search and filter the unified product index with image thumbnails.</p>
              </div>

              {/* FILTERS PANEL */}
              <div style={{ display: 'flex', gap: '12px', flexWrap: 'wrap', background: 'rgba(255,255,255,0.02)', padding: '16px', borderRadius: '12px', border: '1px solid rgba(255,255,255,0.05)' }}>
                <input 
                  type="text" 
                  placeholder="Search title, ID, SKU..." 
                  value={searchTerm} 
                  onChange={(e) => setSearchTerm(e.target.value)} 
                  style={{ flex: 1, minWidth: '200px', background: 'var(--bg-primary)', border: '1px solid rgba(255,255,255,0.1)', padding: '8px 12px', borderRadius: '8px' }}
                />
                <select value={categoryFilter} onChange={(e) => setCategoryFilter(e.target.value)} style={{ background: 'var(--bg-primary)', border: '1px solid rgba(255,255,255,0.1)', padding: '8px 12px', borderRadius: '8px' }}>
                  <option value="All">All Categories</option>
                  <option value="Pokémon">Pokémon Cards</option>
                  <option value="Stamps">Stamps</option>
                  <option value="Comics">Comics</option>
                  <option value="Coins">Coins</option>
                </select>
                <select value={statusFilter} onChange={(e) => setStatusFilter(e.target.value)} style={{ background: 'var(--bg-primary)', border: '1px solid rgba(255,255,255,0.1)', padding: '8px 12px', borderRadius: '8px' }}>
                  <option value="All">All Statuses</option>
                  <option value="Currently Listed">Listed Currently</option>
                  <option value="Listed within 30 days">Listed within last 30 days</option>
                  <option value="Never Listed">Never Listed / Draft</option>
                </select>
                <select value={formatFilter} onChange={(e) => setFormatFilter(e.target.value)} style={{ background: 'var(--bg-primary)', border: '1px solid rgba(255,255,255,0.1)', padding: '8px 12px', borderRadius: '8px' }}>
                  <option value="All">All Formats</option>
                  <option value="Buy It Now">Buy It Now</option>
                  <option value="Auction">Auction</option>
                  <option value="Lot">Lot Assignment</option>
                  <option value="Single">Single Item</option>
                </select>
              </div>

              {/* GRID */}
              <div style={{ display: 'grid', gridTemplateColumns: '1fr', gap: '12px' }}>
                {filteredInventory.map(item => (
                  <div key={item.id} className="inventory-row" style={{ display: 'flex', gap: '16px', background: 'rgba(255,255,255,0.03)', padding: '12px', borderRadius: '12px', border: '1px solid rgba(255,255,255,0.05)', alignItems: 'center', transition: 'var(--transition-smooth)' }}>
                    
                    {/* THUMBNAIL WITH ZOOM */}
                    <div className="thumbnail-container" style={{ position: 'relative' }}>
                      <img src={item.thumbnail} alt="thumbnail" style={{ width: '60px', height: '60px', borderRadius: '8px', objectFit: 'cover', border: '1px solid rgba(255,255,255,0.1)' }} />
                      <div className="thumbnail-popup">
                        <img src={item.thumbnail} alt="zoom" style={{ width: '180px', height: '180px', borderRadius: '12px', objectFit: 'cover', boxShadow: '0 10px 30px rgba(0,0,0,0.8)' }} />
                      </div>
                    </div>

                    <div style={{ flex: 1 }}>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                        <span style={{ fontSize: '12px', color: 'var(--accent-secondary)', fontWeight: 'bold' }}>{item.id}</span>
                        <span style={{ fontSize: '11px', background: 'rgba(255,255,255,0.08)', padding: '2px 6px', borderRadius: '4px' }}>{item.category}</span>
                      </div>
                      <h4 style={{ margin: '4px 0', fontSize: '15px' }}>{item.title}</h4>
                      <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>{item.details}</p>
                    </div>

                    <div style={{ textAlign: 'right', marginRight: '16px' }}>
                      <div style={{ fontWeight: 'bold', fontSize: '16px', color: 'var(--accent-gold)' }}>${item.price.toFixed(2)}</div>
                      <span style={{ fontSize: '11px', color: 'var(--text-muted)' }}>{item.format}</span>
                    </div>

                    <div style={{ display: 'flex', flexDirection: 'column', gap: '6px', alignItems: 'flex-end' }}>
                      <span style={{ fontSize: '11px', background: item.status.includes('Listed') ? 'rgba(46,213,115,0.15)' : 'rgba(255,71,87,0.15)', color: item.status.includes('Listed') ? 'var(--accent-success)' : 'var(--accent-danger)', padding: '4px 8px', borderRadius: '6px', fontWeight: '600' }}>
                        {item.status}
                      </span>
                      <div style={{ display: 'flex', gap: '6px' }}>
                        <button onClick={() => handleOptimize(item)} title="Optimize Listing Details" style={{ background: 'transparent', border: '1px solid rgba(255,255,255,0.15)', color: 'var(--accent-secondary)', padding: '4px 8px', borderRadius: '6px', cursor: 'pointer', fontSize: '11px' }}>
                          Optimize
                        </button>
                        <button onClick={() => handleSocialShare(item)} title="Share to Social Medias" style={{ background: 'transparent', border: '1px solid rgba(255,255,255,0.15)', color: 'var(--text-main)', padding: '4px 8px', borderRadius: '6px', cursor: 'pointer', fontSize: '11px' }}>
                          Share
                        </button>
                      </div>
                    </div>

                  </div>
                ))}
              </div>
            </div>
          )}

          {/* TAB 3: AI IMPORT REVIEW */}
          {activeTab === 'ingest' && (
            <div className="animate-slide" style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
              <div>
                <h2>AI Import & Ingestion Dashboard</h2>
                <p style={{ color: 'var(--text-muted)' }}>Review incoming photo items. Correct bounding box predictions to retrain models.</p>
              </div>

              {ingestQueue.length === 0 ? (
                <div style={{ textAlign: 'center', padding: '60px 0' }}>
                  <CheckCircle size={48} style={{ color: 'var(--accent-success)', marginBottom: '16px' }} />
                  <h3>Ingestion Queue Clear!</h3>
                  <p style={{ color: 'var(--text-muted)' }}>All imported items have been categorized and synced to the database.</p>
                  {pairedDevice && (
                    <button onClick={handleSimulateMobileUpload} className="glowing-button" style={{ marginTop: '16px' }}>Simulate New Mobile Upload</button>
                  )}
                </div>
              ) : (
                <div style={{ display: 'grid', gridTemplateColumns: '1fr 380px', gap: '20px' }}>
                  
                  {/* Left Column: Image with Bounding Box Overlay */}
                  <div style={{ background: 'rgba(0,0,0,0.3)', padding: '16px', borderRadius: '16px', display: 'flex', flexDirection: 'column', gap: '16px' }}>
                    <div style={{ position: 'relative', width: '100%', height: '350px', background: '#000', borderRadius: '8px', overflow: 'hidden', display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
                      <img src={selectedQueueItem.image} alt="Ingested object" style={{ maxHeight: '100%', maxWidth: '100%', objectFit: 'contain' }} />
                      
                      {/* Bounding Box Simulation */}
                      <div style={{ 
                        position: 'absolute', 
                        border: '2px dashed var(--accent-secondary)', 
                        boxShadow: '0 0 10px rgba(0, 242, 254, 0.4)',
                        top: '15%',
                        left: '25%',
                        width: '50%',
                        height: '70%',
                        display: 'flex',
                        flexDirection: 'column',
                        justifyContent: 'space-between',
                        padding: '6px'
                      }}>
                        <span style={{ background: 'var(--accent-secondary)', color: '#000', fontSize: '10px', fontWeight: 'bold', padding: '2px 4px', alignSelf: 'flex-start', borderRadius: '2px' }}>
                          {selectedQueueItem.detectedObject} ({(selectedQueueItem.confidence * 100).toFixed(0)}%)
                        </span>
                      </div>
                    </div>
                    
                    {/* Queue Selection List */}
                    <div style={{ display: 'flex', gap: '12px', overflowX: 'auto', paddingBottom: '8px' }}>
                      {ingestQueue.map(item => (
                        <div key={item.id} onClick={() => setSelectedQueueItem(item)} style={{ cursor: 'pointer', flexShrink: 0, border: selectedQueueItem.id === item.id ? '2px solid var(--accent-secondary)' : '2px solid transparent', borderRadius: '8px', overflow: 'hidden' }}>
                          <img src={item.image} alt="preview" style={{ width: '80px', height: '80px', objectFit: 'cover' }} />
                        </div>
                      ))}
                    </div>
                  </div>

                  {/* Right Column: Editing and AI Training */}
                  <div style={{ background: 'rgba(255,255,255,0.02)', padding: '20px', borderRadius: '16px', border: '1px solid rgba(255,255,255,0.06)', display: 'flex', flexDirection: 'column', gap: '16px' }}>
                    <h3>Ingest Configuration</h3>
                    
                    <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
                      <label style={{ fontSize: '12px', color: 'var(--text-muted)' }}>Identified Item Name</label>
                      <input 
                        type="text" 
                        id="corrected-title"
                        defaultValue={selectedQueueItem.suggestedTitle}
                        key={selectedQueueItem.id + "-title"}
                        style={{ background: 'var(--bg-primary)', border: '1px solid rgba(255,255,255,0.1)', padding: '10px', borderRadius: '8px' }}
                      />
                    </div>

                    <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
                      <label style={{ fontSize: '12px', color: 'var(--text-muted)' }}>Category Destination</label>
                      <select 
                        id="corrected-category" 
                        defaultValue={selectedQueueItem.suggestedCategoryId} 
                        key={selectedQueueItem.id + "-cat"}
                        style={{ background: 'var(--bg-primary)', border: '1px solid rgba(255,255,255,0.1)', padding: '10px', borderRadius: '8px' }}
                      >
                        {EBAY_CATEGORIES.map(cat => (
                          <option key={cat.id} value={cat.id}>{cat.name}</option>
                        ))}
                      </select>
                    </div>

                    <div style={{ background: 'rgba(138,43,226,0.08)', border: '1px solid rgba(138,43,226,0.2)', padding: '12px', borderRadius: '8px', fontSize: '12px', color: 'var(--text-muted)' }}>
                      <span style={{ fontWeight: 'bold', color: 'var(--text-main)', display: 'block', marginBottom: '4px' }}>AI Model Update Hook</span>
                      If you edit this categorization data, the app will automatically feed this template back to train the similarity and Tesseract vocabulary models.
                    </div>

                    <button 
                      onClick={() => {
                        const titleVal = document.getElementById('corrected-title').value;
                        const selectEl = document.getElementById('corrected-category');
                        const categoryVal = selectEl.options[selectEl.selectedIndex].text;
                        const catId = selectEl.value;
                        handleUpdateAndTrain(titleVal, categoryVal, catId);
                      }} 
                      className="glowing-button" 
                      style={{ padding: '12px', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px' }}
                    >
                      <Check size={18} /> Approve & Train Model
                    </button>
                  </div>

                </div>
              )}
            </div>
          )}

          {/* TAB 4: SEED SORTING */}
          {activeTab === 'sorter' && (
            <div className="animate-slide" style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
              <div>
                <h2>Seed-based Visual Image Sorting</h2>
                <p style={{ color: 'var(--text-muted)' }}>Upload templates of trading cards, stamps, or comics, and use them as seeds for classifying large photo folders.</p>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '16px' }}>
                {seeds.map(seed => (
                  <div key={seed.id} className="glass" style={{ padding: '16px', display: 'flex', gap: '12px', alignItems: 'center', background: 'rgba(255,255,255,0.02)' }}>
                    <img src={seed.sample} alt="seed" style={{ width: '70px', height: '70px', borderRadius: '8px', objectFit: 'cover' }} />
                    <div>
                      <h4 style={{ color: 'var(--accent-secondary)' }}>{seed.name}</h4>
                      <p style={{ fontSize: '12px', color: 'var(--text-muted)' }}>Mapping Target: {seed.desc}</p>
                    </div>
                  </div>
                ))}
              </div>

              {sortProgress ? (
                <div style={{ background: 'rgba(0,0,0,0.3)', padding: '30px', borderRadius: '16px', textAlign: 'center', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '12px' }}>
                  <RefreshCw className="spinning" size={32} style={{ color: 'var(--accent-secondary)' }} />
                  <div style={{ fontWeight: 'bold' }}>{sortProgress}</div>
                  <div style={{ width: '200px', height: '6px', background: 'rgba(255,255,255,0.1)', borderRadius: '3px', overflow: 'hidden' }}>
                    <div style={{ width: '60%', height: '100%', background: 'linear-gradient(90deg, var(--accent-secondary), var(--accent-primary))' }}></div>
                  </div>
                </div>
              ) : (
                <div style={{ background: 'rgba(138,43,226,0.05)', border: '1px dashed rgba(138,43,226,0.3)', padding: '24px', borderRadius: '16px', display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '12px', textAlign: 'center' }}>
                  <Sparkles size={32} style={{ color: 'var(--accent-primary)' }} />
                  <h3>Run Automatic Image Sorting</h3>
                  <p style={{ color: 'var(--text-muted)', maxWidth: '500px', fontSize: '13px' }}>The system will run perceptual visual comparisons matching all newly scanned local images to the closest seed template, applying categories automatically.</p>
                  <button onClick={handleRunSeedSort} className="glowing-button" style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <RefreshCw size={16} /> Run Seed Sorting on Photo Roots
                  </button>
                </div>
              )}
            </div>
          )}

          {/* TAB 5: IMAGE EDITOR */}
          {activeTab === 'editor' && (
            <div className="animate-slide" style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
              <div>
                <h2>Canvas Image Editor</h2>
                <p style={{ color: 'var(--text-muted)' }}>Perform local rotations and crops. Remove backgrounds instantly or route to eBay editing fallback.</p>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: '1fr 320px', gap: '20px' }}>
                
                {/* Editor Surface */}
                <div style={{ background: '#000', borderRadius: '16px', display: 'flex', flexDirection: 'column', alignItems: 'center', justifyContent: 'center', padding: '24px', minHeight: '380px', position: 'relative' }}>
                  {isRemovingBg ? (
                    <div style={{ display: 'flex', flexDirection: 'column', alignItems: 'center', gap: '12px' }}>
                      <RefreshCw size={36} className="spinning" style={{ color: 'var(--accent-secondary)' }} />
                      <span>Processing AI Background Extraction...</span>
                    </div>
                  ) : (
                    <canvas ref={canvasRef} style={{ background: bgRemoved ? 'radial-gradient(#ccc 25%, transparent 25%), radial-gradient(#ccc 25%, transparent 25%) 10px 10px / 20px 20px' : 'transparent', borderRadius: '8px', border: '1px solid rgba(255,255,255,0.1)' }}></canvas>
                  )}
                </div>

                {/* Edit Controls */}
                <div style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
                  <div className="glass" style={{ padding: '16px', background: 'rgba(255,255,255,0.02)', display: 'flex', flexDirection: 'column', gap: '12px' }}>
                    <h4>Canvas Actions</h4>
                    
                    <button onClick={() => setRotation(prev => (prev + 90) % 360)} style={{ background: 'rgba(255,255,255,0.08)', border: 'none', padding: '10px', borderRadius: '8px', cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <RotateCw size={16} /> Rotate 90°
                    </button>
                    
                    <button onClick={() => { setCropActive(!cropActive); addLog("Crop grid activated.", "system"); }} style={{ background: cropActive ? 'rgba(0,242,254,0.15)' : 'rgba(255,255,255,0.08)', border: cropActive ? '1px solid var(--accent-secondary)' : 'none', padding: '10px', borderRadius: '8px', cursor: 'pointer', display: 'flex', alignItems: 'center', gap: '8px' }}>
                      <Crop size={16} /> Bounding Box Crop
                    </button>

                    <button onClick={handleRemoveBg} className="glowing-button" style={{ display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '8px' }}>
                      <Palette size={16} /> Remove Background
                    </button>
                  </div>

                  <div className="glass" style={{ padding: '16px', background: 'rgba(255,255,255,0.01)', fontSize: '12px', display: 'flex', flexDirection: 'column', gap: '10px' }}>
                    <div style={{ fontWeight: 'bold' }}>eBay Edit Fallback</div>
                    <p style={{ color: 'var(--text-muted)' }}>If the automated background removal needs refinement, route the image details directly to eBay's photo manager after saving.</p>
                    <button onClick={() => addLog("Redirect token created. User redirected to eBay Image tools.", "system")} style={{ background: 'transparent', border: '1px solid rgba(255,255,255,0.2)', padding: '8px', borderRadius: '8px', cursor: 'pointer', display: 'flex', alignItems: 'center', justifyContent: 'center', gap: '4px' }}>
                      Use eBay's Image Editor <ExternalLink size={12} />
                    </button>
                  </div>
                </div>

              </div>
            </div>
          )}

          {/* TAB 6: EBAY CATEGORIES */}
          {activeTab === 'categories' && (
            <div className="animate-slide" style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
              <div>
                <h2>eBay Category Catalog</h2>
                <p style={{ color: 'var(--text-muted)' }}>Explore official eBay category nodes and match IDs for listing mapping.</p>
              </div>

              <div style={{ background: 'rgba(255,255,255,0.02)', padding: '20px', borderRadius: '16px', border: '1px solid rgba(255,255,255,0.06)' }}>
                <input 
                  type="text" 
                  placeholder="Search categories (e.g. trading cards, comics, stamps)..." 
                  style={{ width: '100%', background: 'var(--bg-primary)', border: '1px solid rgba(255,255,255,0.1)', padding: '12px', borderRadius: '8px', marginBottom: '20px' }}
                />
                
                <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                  {EBAY_CATEGORIES.map(cat => (
                    <div key={cat.id} style={{ display: 'flex', justifyContent: 'space-between', padding: '12px', background: 'rgba(0,0,0,0.2)', borderRadius: '8px', alignItems: 'center' }}>
                      <span style={{ fontWeight: '600' }}>{cat.name}</span>
                      <span style={{ background: 'rgba(0,242,254,0.15)', color: 'var(--accent-secondary)', padding: '4px 10px', borderRadius: '6px', fontSize: '12px', fontWeight: 'bold' }}>ID: {cat.id}</span>
                    </div>
                  ))}
                </div>
              </div>
            </div>
          )}

          {/* TAB 7: SOCIAL MEDIA */}
          {activeTab === 'social' && (
            <div className="animate-slide" style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
              <div>
                <h2>Social Media Share Center</h2>
                <p style={{ color: 'var(--text-muted)' }}>Link accounts and broadcast active listings directly to social channels.</p>
              </div>

              <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(220px, 1fr))', gap: '16px' }}>
                <div className="glass" style={{ padding: '16px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#1877f2" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M18 2h-3a5 5 0 0 0-5 5v3H7v4h3v8h4v-8h3l1-4h-4V7a1 1 0 0 1 1-1h3z"/></svg>
                    <span>Facebook</span>
                  </div>
                  <input type="checkbox" checked={socialAccounts.facebook} onChange={() => setSocialAccounts(p => ({...p, facebook: !p.facebook}))} />
                </div>
                <div className="glass" style={{ padding: '16px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#e1306c" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><rect x="2" y="2" width="20" height="20" rx="5" ry="5"/><path d="M16 11.37A4 4 0 1 1 12.63 8 4 4 0 0 1 16 11.37z"/><line x1="17.5" y1="6.5" x2="17.51" y2="6.5"/></svg>
                    <span>Instagram</span>
                  </div>
                  <input type="checkbox" checked={socialAccounts.instagram} onChange={() => setSocialAccounts(p => ({...p, instagram: !p.instagram}))} />
                </div>
                <div className="glass" style={{ padding: '16px', display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
                  <div style={{ display: 'flex', gap: '8px', alignItems: 'center' }}>
                    <svg width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="#1da1f2" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M22 4s-.7 2.1-2 3.4c1.6 10-9.4 17.3-18 11.6 2.2.1 4.4-.6 6-2C3 15.5.5 9.6 3 5c2.2 2.6 5.6 4.1 9 4-.9-4.2 4-6.6 7-3.8 1.1 0 3-1.2 3-1.2z"/></svg>
                    <span>Twitter/X</span>
                  </div>
                  <input type="checkbox" checked={socialAccounts.twitter} onChange={() => setSocialAccounts(p => ({...p, twitter: !p.twitter}))} />
                </div>
              </div>

              <div style={{ background: 'rgba(0,0,0,0.2)', padding: '20px', borderRadius: '12px' }}>
                <h4>Choose Listings to Share</h4>
                <p style={{ fontSize: '12px', color: 'var(--text-muted)', marginBottom: '12px' }}>Select share action directly on the inventory log tab to draft cross-network posts.</p>
              </div>
            </div>
          )}

          {/* TAB 8: EBAY CONNECT / OAUTH */}
          {activeTab === 'connect' && (
            <div className="animate-slide" style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
              <div>
                <h2>eBay Connection Manager</h2>
                <p style={{ color: 'var(--text-muted)' }}>Authorize eBay Hero to access your seller account using the official OAuth 2.0 flow. Credentials are stored locally only.</p>
              </div>
              <EbayOAuthPanel onStatusChange={(status) => {
                setEbayStatus(status);
                addLog(
                  status === 'connected' ? 'eBay OAuth: Account connected successfully.' : 'eBay OAuth: Account disconnected.',
                  status === 'connected' ? 'success' : 'system'
                );
              }} />
            </div>
          )}

          {/* SOCIAL SHARE DIALOG OVERLAY */}
          {shareStatus && (
            <div style={{ position: 'fixed', top: 0, left: 0, width: '100vw', height: '100vh', background: 'rgba(0,0,0,0.7)', zIndex: 1000, display: 'flex', alignItems: 'center', justifyContent: 'center' }}>
              <div className="glass" style={{ width: '450px', padding: '24px', background: 'var(--bg-secondary)', display: 'flex', flexDirection: 'column', gap: '16px' }}>
                <h3>Broadcast Live Listing</h3>
                <div style={{ display: 'flex', gap: '12px', background: 'rgba(0,0,0,0.2)', padding: '12px', borderRadius: '8px' }}>
                  <img src={shareStatus.item.thumbnail} alt="" style={{ width: '60px', height: '60px', objectFit: 'cover', borderRadius: '6px' }} />
                  <div>
                    <div style={{ fontWeight: 'bold', fontSize: '14px' }}>{shareStatus.item.title}</div>
                    <div style={{ color: 'var(--accent-gold)', fontSize: '13px' }}>${shareStatus.item.price.toFixed(2)}</div>
                  </div>
                </div>

                <div style={{ display: 'flex', flexDirection: 'column', gap: '6px' }}>
                  <label style={{ fontSize: '12px', color: 'var(--text-muted)' }}>Generated Social Copy</label>
                  <textarea 
                    rows={4}
                    defaultValue={`🔥 Live on eBay! Check out this incredible ${shareStatus.item.title} available now for only $${shareStatus.item.price}. Direct listing link: https://ebay.com/itm/mock-item-id`}
                    style={{ background: 'var(--bg-primary)', border: '1px solid rgba(255,255,255,0.1)', padding: '10px', borderRadius: '8px', fontSize: '13px', color: 'var(--text-main)' }}
                  />
                </div>

                <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '10px' }}>
                  <button onClick={() => setShareStatus(null)} style={{ background: 'rgba(255,255,255,0.1)', border: 'none', padding: '8px 16px', borderRadius: '8px', cursor: 'pointer' }}>Cancel</button>
                  <button onClick={executeShare} className="glowing-button">Post Now</button>
                </div>
              </div>
            </div>
          )}

        </main>
      </div>

      {/* FOOTER */}
      <footer style={{ padding: '12px', textAlign: 'center', fontSize: '12px', color: 'var(--text-muted)', borderTop: '1px solid rgba(255,255,255,0.04)' }}>
        © 2026 eBay Hero Command Center. All Rights Reserved. Platform Console v1.0.0 (PWA Mode Active)
      </footer>

      {/* CUSTOM STYLE ELEMENT */}
      <style dangerouslySetInnerHTML={{__html: `
        .nav-btn {
          display: flex;
          align-items: center;
          gap: 12px;
          background: transparent;
          border: none;
          padding: 12px 16px;
          border-radius: 8px;
          text-align: left;
          cursor: pointer;
          font-weight: 500;
          font-size: 14px;
          transition: var(--transition-smooth);
        }
        .nav-btn:hover {
          background: rgba(255,255,255,0.05);
          color: var(--accent-secondary);
        }
        .nav-btn.active {
          background: rgba(138,43,226,0.15);
          color: var(--accent-secondary);
          border-left: 3px solid var(--accent-secondary);
        }
        .stat-card {
          background: var(--glass-bg);
          border: 1px solid var(--glass-border);
          padding: 16px;
          border-radius: 12px;
          box-shadow: 0 4px 10px rgba(0,0,0,0.2);
        }
        .stat-label {
          font-size: 11px;
          text-transform: uppercase;
          color: var(--text-muted);
          font-weight: 600;
          letter-spacing: 0.05em;
        }
        .stat-value {
          font-size: 28px;
          font-family: var(--font-display);
          font-weight: 800;
          margin: 4px 0;
          background: linear-gradient(to right, #fff, var(--text-muted));
          -webkit-background-clip: text;
          -webkit-text-fill-color: transparent;
        }
        .stat-sub {
          font-size: 11px;
          color: var(--accent-secondary);
        }
        .inventory-row:hover {
          background: rgba(255,255,255,0.06) !important;
          border-color: rgba(0,242,254,0.2) !important;
          transform: scale(1.002);
        }
        .thumbnail-popup {
          display: none;
          position: absolute;
          left: 70px;
          top: -60px;
          z-index: 200;
        }
        .thumbnail-container:hover .thumbnail-popup {
          display: block;
        }
        @keyframes spin {
          from { transform: rotate(0deg); }
          to { transform: rotate(360deg); }
        }
        .spinning {
          animation: spin 2s linear infinite;
        }
      `}} />

    </div>
  );
}
