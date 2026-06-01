import { useState } from 'react';
import './App.css';
import SellPlanManager from './components/SellPlanManager';

function App() {
  const [activeTab, setActiveTab] = useState('dashboard');

  return (
    <div className="app-container">
      <header>
        <h1>AutoInvesting Dashboard</h1>
        <div style={{ marginTop: '10px' }}>
          <button 
            style={{ marginRight: '10px', backgroundColor: activeTab === 'dashboard' ? 'var(--primary-color)' : '#6c757d' }}
            onClick={() => setActiveTab('dashboard')}
          >
            Overview
          </button>
          <button 
            style={{ backgroundColor: activeTab === 'sellplans' ? 'var(--primary-color)' : '#6c757d' }}
            onClick={() => setActiveTab('sellplans')}
          >
            Sell Plans (분할매도)
          </button>
        </div>
      </header>

      <main>
        {activeTab === 'dashboard' && (
          <div className="card">
            <h2>System Status</h2>
            <p>Phase C Headless API is running.</p>
            <p>Select "Sell Plans" to manage your split-sell strategies.</p>
          </div>
        )}
        
        {activeTab === 'sellplans' && <SellPlanManager />}
      </main>
    </div>
  );
}

export default App;
